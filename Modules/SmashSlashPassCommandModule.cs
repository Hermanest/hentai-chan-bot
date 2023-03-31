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
        private async Task HandleCommand(IAttachment attachment, string? artists = null, string? characters = null, string? description = null) {
            _logger?.LogDebug("Initiated smash-slash-pass command");
            await DeferAsync(true);

            _logger?.LogDebug("Attempting to get config...");
            var conf = await GetContextConfigAsync();
            if (conf == null) {
                await ModifyWithErrorAsync("Failed to load configuration");
                return;
            }

            _logger?.LogDebug("Attempting to get channel...");
            var channel = await Context.Guild.GetChannelAsync(conf.ChannelId);
            if (channel == null) {
                await ModifyWithErrorAsync("Invalid channel specified");
                return;
            }

            _logger?.LogDebug("Validating channel...");
            if (channel.GetChannelType() is not ChannelType.Text) {
                await ModifyWithErrorAsync("Channel should be generic text channel");
                return;
            }

            _logger?.LogDebug("Attempting to parse emotes...");
            var msgChannel = channel as IMessageChannel;
            if (!DiscordUtils.TryParseEmote(conf.SmashEmote, out var smashEmote)
                || !DiscordUtils.TryParseEmote(conf.PassEmote, out var passEmote)) {
                await ModifyWithErrorAsync("Invalid emotes specified");
                return;
            }

            _logger?.LogDebug("Attempting to remove original message...");
            await DeleteOriginalResponseAsync();

            _logger?.LogDebug("Sending response...");
            var sentMsg = await msgChannel!.SendMessageAsync(embed:
                BuildEmbed(attachment, Context.User, artists, characters, description));

            _logger?.LogDebug("Adding reactions...");
            await sentMsg.AddReactionsAsync(new[] {
                smashEmote, passEmote
            });
            _logger?.LogDebug("Command finished");
        }

        private static Embed BuildEmbed(IAttachment attachment, IUser user, string? artists, string? characters, string? description) => 
            EmbedUtils.BuildImageEmbed(null, user, Color.Blue, attachment.Url, null, artists, characters, description);
    }
}