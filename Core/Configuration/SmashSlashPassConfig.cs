using Discord;
using Discord.Addons.SetupModule;
using JetBrains.Annotations;

namespace HentaiChanBot.Configuration {
    [ComplexObject("smash-slash-pass"), UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal class SmashSlashPassConfig {
        public ulong ChannelId { get; set; }
        public ulong? CacheChannelId { get; set; }
        public string? SmashEmote { get; set; }
        public string? PassEmote { get; set; }

        [ComplexObjectCtor, UsedImplicitly]
        public void Ctor(IChannel? channel = null, string? smash_emote = null, string? pass_emote = null, IChannel? cache_channel = null) {
            ChannelId = channel?.Id ?? ChannelId;
            SmashEmote = smash_emote ?? SmashEmote;
            PassEmote = pass_emote ?? PassEmote;
            CacheChannelId = cache_channel?.Id ?? CacheChannelId;
        }
    }
}