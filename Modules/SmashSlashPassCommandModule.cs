using Discord;
using Discord.Addons;
using Discord.Addons.Utils;
using Discord.Interactions;
using Discord.WebSocket;
using HentaiChanBot.Configuration;
using HentaiChanBot.Utils;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace HentaiChanBot.Modules {
    internal class SmashSlashPassCommandModule : ConfigurableInteractionModuleBase<SmashSlashPassConfig> {
        //discord.net creates a new instance of the class every time the command is called, that's why we can
        //store such things like `_selectorId` without having any problems because of asynchronous or multi-threaded code

        public SmashSlashPassCommandModule(
            DiscordSocketClient client,
            ILogger<SmashSlashPassCommandModule>? logger = null) {
            _client = client;
            _logger = logger;
            _client.SelectMenuExecuted += HandleSelectMenuExecuted;
        }

        ~SmashSlashPassCommandModule() {
            _client.SelectMenuExecuted -= HandleSelectMenuExecuted;
        }

        private readonly DiscordSocketClient _client;
        private readonly ILogger? _logger;

        private IUser? _originalMessageAuthor;
        private string? _selectorId;

        protected override Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func, RequestOptions? options = null) {
            return base.ModifyOriginalResponseAsync(x => {
                x.Components = null;
                x.Embeds = null;
                func(x);
            }, options);
        }

        #region smash-slash-pass
        
        [SlashCommand("smash-slash-pass", "Sends voting post to the smash-slash-pass channel"), UsedImplicitly]
        private Task HandleCommandWrapper(
            IAttachment? attachment = null,
            string? link = null,
            string? artists = null,
            string? characters = null,
            string? description = null
        ) => HandleCommand(
            attachment,
            link,
            artists,
            characters,
            description
        );

        private async Task HandleCommand(
            IAttachment? attachment = null,
            string? link = null,
            string? artists = null,
            string? characters = null,
            string? description = null,
            bool isDirectCall = true
        ) {
            static Embed BuildEmbed(string imageUrl, IUser user, string? artists, string? characters, string? description, IUser? originalMessageAuthor) =>
                EmbedUtils.BuildImageEmbed(
                    null,
                    user,
                    Color.Blue,
                    imageUrl,
                    null,
                    artists,
                    characters,
                    description,
                    originalMessageAuthor
                );

            _logger?.LogDebug("Initiated");
            if (isDirectCall) await DeferAsync(true);

            _logger?.LogDebug("Validating publication data...");
            if (attachment is null && link is null) {
                await ModifyWithErrorAsync("At least one content field should be filled");
                return;
            }
            link ??= attachment!.Url;
            if (!UriUtils.ValidateImageLink(link)) {
                await ModifyWithErrorAsync("The content link is invalid");
                return;
            }

            _logger?.LogDebug("Attempting to get config...");
            if (await GetContextConfigAsync() is not { } conf) {
                await ModifyWithErrorAsync("Failed to load configuration: The module probably was not configured by server owner");
                return;
            }

            _logger?.LogDebug("Attempting to get channel...");
            if (await Context.Guild.GetChannelAsync(conf.ChannelId) is not { } channel) {
                await ModifyWithErrorAsync("Invalid channel specified");
                return;
            }

            _logger?.LogDebug("Validating channel...");
            if (channel.GetChannelType() is not ChannelType.Text) {
                await ModifyWithErrorAsync("Channel should be generic text channel");
                return;
            }

            _logger?.LogDebug("Attempting to parse emotes...");
            if (!DiscordUtils.TryParseEmote(conf.SmashEmote, out var smashEmote)
                || !DiscordUtils.TryParseEmote(conf.PassEmote, out var passEmote)) {
                await ModifyWithErrorAsync("Invalid emotes specified");
                return;
            }

            var msgChannel = channel as IMessageChannel;
            _logger?.LogDebug("Attempting to publish...");
            var sentMsg = await msgChannel!.SendMessageAsync(embed:
                BuildEmbed(link!, Context.User, artists, characters,
                    description, isDirectCall ? null : _originalMessageAuthor));

            _logger?.LogDebug("Adding reactions...");
            await sentMsg.AddReactionsAsync(new[] {
                smashEmote, passEmote
            });

            _logger?.LogDebug("Attempting to modify original message...");

            await ModifyOriginalResponseAsync(x => x.Embed =
                DiscordUtils.BuildSuccessEmbed($"Successfully published to <#{msgChannel.Id}>"));
            _logger?.LogDebug("Finished");
        }
        
        #endregion

        #region Smash Slash Pass (message)
        
        [MessageCommand("Smash Slash Pass"), UsedImplicitly]
        private async Task HandleMessageCommand(
            IMessage msg
        ) {
            static Embed BuildPreviewEmbed(string link, string title) => new EmbedBuilder()
                .WithTitle("Publication preview")
                .WithFields(
                    new EmbedFieldBuilder()
                        .WithName(title)
                        .WithValue($"||{link}||"))
                .WithColor(Color.Blue)
                .WithThumbnailUrl(link)
                .Build();

            await DeferAsync(true);

            var linkTitlePairs = new Dictionary<string, string>()
                .AddRange(UriUtils.FindLinks(msg.Content)
                    .Distinct()
                    .Where(UriUtils.ValidateImageLink)
                    .Select(x => new KeyValuePair<string, string>(x, "Link")))
                .AddRange(msg.Attachments
                    .Select(x => new KeyValuePair<string, string>(x.Url, "Attachment")))
                .AddRange(msg.Embeds
                    .Select(x => x.Image?.Url ?? null)
                    .Where(x => x is not null)
                    .Select(x => new KeyValuePair<string, string>(x!, "Embed")));

            _originalMessageAuthor = msg.Author;
            switch (linkTitlePairs.Count) {
                default:
                    await ModifyWithErrorAsync("Not found any valid links in the message");
                    break;
                case > 1:
                    static string MakeTitle(string x, int i) => $"{x} ({i + 1})";

                    _selectorId = $"selection-dialog-selector-{Context.Interaction.Id}";
                    await ModifyOriginalResponseAsync(x => {
                        x.Embeds = new Optional<Embed[]>(linkTitlePairs
                            .Select((s, i) =>
                                BuildPreviewEmbed(s.Key, MakeTitle(s.Value, i)))
                            .ToArray());
                        x.Components = new Optional<MessageComponent>(new ComponentBuilder()
                            .AddRow(new ActionRowBuilder()
                                .WithSelectMenu(_selectorId, linkTitlePairs
                                    .Select((s, i) => new SelectMenuOptionBuilder()
                                        .WithLabel(MakeTitle(s.Value, i))
                                        .WithValue(MakeTitle(s.Value, i)))
                                    .ToList(), "Select an option"))
                            .Build());
                    });
                    break;
                case 1:
                    await HandleCommand(link: linkTitlePairs.First().Key, isDirectCall: false);
                    break;
            }
        }

        private async Task HandleSelectMenuExecuted(SocketMessageComponent component) {
            if (component.Data.CustomId != _selectorId) return;
            await ModifyOriginalResponseAsync(x => x.Embed = DiscordUtils.BuildWaitEmbed());
            var choice = component.Data.Values.FirstOrDefault();
            var fields = component.Message.Embeds.Select(x => x.Fields.FirstOrDefault());
            var link = fields.FirstOrDefault(x => x.Name == choice).Value;
            await HandleCommand(link: link.Replace("||", ""), isDirectCall: false);
            _selectorId = null;
        }
        
        #endregion
    }
}