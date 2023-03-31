﻿using Discord;
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

        private readonly Rule34Api _r34Api;
        private readonly ILogger? _logger;

        [SlashCommand("hentai", "Sends random hentai pic from r34 based on the tags"), NsfwCommand(true), UsedImplicitly]
        public async Task HandleHentaiCommand(string tags) {
            _logger?.LogDebug("Initiated hentai command");
            await DeferAsync();

            _logger?.LogDebug("Attempting to get posts count...");
            var postsCount = await _r34Api.GetPostsCountAsync(tags);
            if (postsCount == 0) {
                await ModifyWithErrorAsync("Unable to find image with specified tags");
                return;
            }

            var randomNumber = new Random().Next(0, postsCount);
            _logger?.LogDebug($"Generated post {randomNumber} from {postsCount} posts");
            _logger?.LogDebug("Attempting to get id...");
            var pid = randomNumber / RULE34_PID_INCREMENT;
            var id = (await _r34Api.GetPostIdsAsync(tags, pid))?
                .ElementAtOrDefault(randomNumber % RULE34_PID_INCREMENT);
            if (id is null) {
                await ModifyWithErrorAsync("Failed to get post id with generated index");
                return;
            }

            _logger?.LogDebug("Attempting to get post...");
            var postViewUrl = _r34Api.GetPostUrl(id.Value);
            var data = await _r34Api.GetPostAsync(id.Value);
            if (data is null) {
                await ModifyWithErrorAsync("Failed to get post with received id");
                return;
            }

            _logger?.LogDebug("Attempting to create embed...");
            try {
                var embed = BuildHentaiEmbed(data.sampleUrl!, postViewUrl,
                    string.Join(", ", data.artists!), string.Join(", ", data.characters!));
                await ModifyOriginalResponseAsync(x => x.Embed = embed);
            } catch (Exception ex) {
                await ModifyWithErrorAsync("Internal error:\r\n" + ex);
                return;
            }
            _logger?.LogDebug("Command completed");
        }

        [SlashCommand("hentai-tag", "Suggests tags from r34 based on the input"), NsfwCommand(true), UsedImplicitly]
        public async Task HandleTagCommand(string input) {
            _logger?.LogDebug("Initiated hentai-tag command");
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
        }

        private static Embed BuildAutocompleteEmbed(IEnumerable<R34Tag> tags) => new EmbedBuilder()
            .WithTitle("Relevant tags:")
            .WithColor(Color.DarkGreen)
            .WithFields(tags
                .TakeWhile((x, y) => y < 25) //hard restriction because of discord's maximum limit
                .Select(x => new EmbedFieldBuilder()
                    .WithName(x.label)
                    .WithValue(x.type ?? "unknown type")
                    .WithIsInline(true)))
            .Build();

        private static Embed BuildHentaiEmbed(string imageUrl, string? postUrl, string artists, string characters) => EmbedUtils
            .BuildImageEmbed("Rule34 Post", null, Color.DarkGreen, imageUrl, postUrl, artists, characters, null);
    }
}