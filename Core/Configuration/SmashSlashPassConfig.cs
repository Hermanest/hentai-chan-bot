using Discord;
using Discord.Addons.SetupModule;
using JetBrains.Annotations;

namespace HentaiChanBot.Configuration {
    [ComplexObject("smash-slash-pass"), UsedImplicitly]
    internal class SmashSlashPassConfig {
        public ulong ChannelId { get; set; }
        public string? SmashEmote { get; set; }
        public string? PassEmote { get; set; }

        [ComplexObjectCtor, UsedImplicitly]
        public void Ctor(IChannel? channel = null, string? smash_emote = null, string? pass_emote = null) {
            ChannelId = channel?.Id ?? ChannelId;
            SmashEmote = smash_emote ?? SmashEmote;
            PassEmote = pass_emote ?? PassEmote;
        }
    }
}