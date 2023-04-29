using Discord;
using Discord.Addons;
using Discord.Interactions;
using HentaiChanBot.API.Rule34;
using HentaiChanBot.Utils;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace HentaiChanBot.Modules {
    internal class HentaiCommandModule : LoggableInteractionModuleBase {
        public HentaiCommandModule(Rule34Api api, ILogger<HentaiCommandModule> logger) {
            _r34Api = api;
            _logger = logger;
        }

        private const int RULE34_PID_INCREMENT = 42;

        private readonly HttpClient _client = new();
        private readonly Rule34Api _r34Api;
        private readonly ILogger? _logger;

        #region hentai

        [SlashCommand("hentai", "Sends random hentai pic from r34 based on the tags"), NsfwCommand(true), UsedImplicitly]
        private async Task HandleHentaiCommand(string tags) {
            _logger?.LogDebug("Initiated hentai");
            await DeferAsync();

            _logger?.LogDebug("Attempting to get posts count...");
            var postsCount = await _r34Api.GetPostsCountAsync(tags);
            if (postsCount is 0) {
                await ModifyWithErrorAsync("Unable to find image with specified tags");
                return;
            }

            var randomNumber = new Random().Next(0, postsCount);
            _logger?.LogDebug($"Chose {randomNumber} from {postsCount} posts");
            _logger?.LogDebug("Attempting to get id...");
            var pid = randomNumber / RULE34_PID_INCREMENT;
            var id = (await _r34Api.GetPostIdsAsync(tags, pid))?
                .ElementAtOrDefault(randomNumber % RULE34_PID_INCREMENT);
            if (id is null) {
                await ModifyWithErrorAsync("Failed to get post id with generated index");
                return;
            }

            _logger?.LogDebug("Attempting to get the post...");
            if (await _r34Api.GetPostAsync(id.Value) is not {} data) {
                await ModifyWithErrorAsync("Failed to get post with received id");
                return;
            }

            _logger?.LogDebug("Attempting to create embed...");
            try {
                var embed = GetHentaiEmbedBuilder(
                    data.sampleUrl!,
                    data.postUrl,
                    string.Join(", ", data.artists!),
                    string.Join(", ", data.characters!));
                var url = data.sampleUrl;
                var canUpload = data.isVideo;
                if (!canUpload || url is null || !await ModifyWithVideoAsync(url, x => x.Embed = embed.Build())) {
                    if (canUpload) embed.WithFooter("❌ Unable to load preview");
                    await ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                }
            } catch (Exception ex) {
                await ModifyWithErrorAsync(ex);
                return;
            }
            _logger?.LogDebug("Finished hentai");
        }

        private async Task<bool> ModifyWithVideoAsync(string url, Action<MessageProperties> callback) {
            try {
                _logger?.LogDebug("Attempting to request video stream...");
                await using var stream = await _client.GetStreamAsync(url);
                _logger?.LogDebug("Attempting to upload file to the discord...");
                await ModifyOriginalResponseAsync(x => {
                    x.Attachments = new[] {
                        new FileAttachment(stream, "video.mp4")
                    };
                    callback(x);
                });
            } catch (Exception) {
                return false;
            }
            return true;
        }

        private static EmbedBuilder GetHentaiEmbedBuilder(string? imageUrl, string? postUrl, string artists, string characters) => EmbedUtils
            .GetImageEmbedBuilder("Rule34 Post", null, Color.DarkGreen, imageUrl, postUrl, artists, characters, null, null);

        #endregion

        #region hentai-tag

        [SlashCommand("hentai-tag", "Suggests tags from r34 based on the input"), NsfwCommand(true), UsedImplicitly]
        private async Task HandleTagCommand(string input) {
            _logger?.LogDebug("Initiated hentai-tag");
            await DeferAsync();
            _logger?.LogDebug("Attempting to get autocomplete tags...");
            var tags = await _r34Api.GetAutocompleteTags(input);
            if (tags is null) {
                await ModifyWithErrorAsync("Failed to get autocomplete tags");
                return;
            }
            if (tags.Length is 0) {
                await ModifyWithErrorAsync("Unable to find relevant tags");
                return;
            }
            await ModifyOriginalResponseAsync(x => x.Embed = BuildAutocompleteEmbed(tags));
            _logger?.LogDebug("Finished hentai-tag");
        }

        private static Embed BuildAutocompleteEmbed(IEnumerable<R34Tag> tags) => new EmbedBuilder()
            .WithTitle("Relevant tags:")
            .WithColor(Color.DarkGreen)
            .WithFields(tags
                .TakeWhile((x, y) => y < 25) //hard restriction because of discord's maximum limit
                .Select(x => new EmbedFieldBuilder()
                    .WithName(x.label?.Replace("_", "\\_"))
                    .WithValue(x.type ?? "unknown type")
                    .WithIsInline(true)))
            .Build();

        #endregion
    }
}