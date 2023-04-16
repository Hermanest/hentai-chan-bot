using Discord;
using Discord.Addons.Utils;

namespace HentaiChanBot.Utils;

public static class EmbedUtils {
    public static Embed BuildImageEmbed(
        string? title,
        IUser? user,
        Color color,
        string imageUrl,
        string? titleUrl,
        string? artists,
        string? characters,
        string? description)
        => GetImageEmbedBuilder(title,
            user, color, imageUrl,
            titleUrl, artists, characters,
            description).Build();

    public static EmbedBuilder GetImageEmbedBuilder(
        string? title,
        IUser? user,
        Color color,
        string? imageUrl,
        string? titleUrl,
        string? artists,
        string? characters,
        string? description)
        => new EmbedBuilder()
            .If(!string.IsNullOrWhiteSpace(title),
                x => x.WithTitle(title))
            .If(user is not null, x => x
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(user!.Username)
                    .WithIconUrl(user.GetAvatarUrl())))
            .If(!string.IsNullOrWhiteSpace(imageUrl), 
                x=> x.WithImageUrl(imageUrl))
            .WithUrl(titleUrl)
            .WithColor(color)
            .WithFields(new List<EmbedFieldBuilder>()
                .AppendIf(!string.IsNullOrWhiteSpace(artists),
                    () => new EmbedFieldBuilder()
                        .WithName("Artists")
                        .WithValue(artists))
                .AppendIf(!string.IsNullOrWhiteSpace(characters),
                    () => new EmbedFieldBuilder()
                        .WithName("Characters")
                        .WithValue(characters))
                .AppendIf(!string.IsNullOrWhiteSpace(description),
                    () => new EmbedFieldBuilder()
                        .WithName("Description")
                        .WithValue(description)));
}