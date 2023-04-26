using Discord;
using Discord.Addons;
using Discord.Addons.Utils;
using Discord.Interactions;
using HentaiChanBot.Configuration;
using HentaiChanBot.Utils;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace HentaiChanBot.Modules {
    internal class SmashSlashPassCommandModule : ConfigurableInteractionModuleBase<SmashSlashPassConfig> {
        public SmashSlashPassCommandModule(ILogger<SmashSlashPassCommandModule>? logger = null) {
            _logger = logger;
        }

        private readonly ILogger? _logger;

        [SlashCommand("smash-slash-pass", "Sends voting post to the smash-slash-pass channel"), UsedImplicitly]
        private async Task HandleCommand(
            IAttachment? attachment = null,
            string? link = null,
            string? artists = null,
            string? characters = null,
            string? description = null
        ) {
            _logger?.LogDebug("Initiated");
            await DeferAsync(true);

            _logger?.LogDebug("Validating publication data...");
            if (attachment is null && link is null) {
                await ModifyWithErrorAsync("At least one content field should be filled");
                return;
            }
            link ??= attachment!.Url;
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) {
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
                BuildEmbed(link!, Context.User, artists, characters, description));

            _logger?.LogDebug("Adding reactions...");
            await sentMsg.AddReactionsAsync(new[] {
                smashEmote, passEmote
            });

            _logger?.LogDebug("Attempting to modify original message...");

            await ModifyOriginalResponseAsync(x => x
                .Embed = DiscordUtils.BuildSuccessEmbed($"Successfully published to <#{msgChannel.Id}>"));
            _logger?.LogDebug("Finished");
        }

        private static Embed BuildEmbed(string imageUrl, IUser user, string? artists, string? characters, string? description) =>
            EmbedUtils.BuildImageEmbed(null, user, Color.Blue, imageUrl, null, artists, characters, description);
    }
}