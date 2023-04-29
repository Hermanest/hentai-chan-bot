using System.Text.RegularExpressions;

namespace HentaiChanBot.Utils; 

public static class UriUtils {
    private static readonly Regex _imageLinkRegex = new(@"^https?://\S+\.(png|jpe?g|gif|webp)(\?\S+)?$");
    private static readonly Regex _linkRegex = new(@"\bhttps?://\S+");
    
    public static bool ValidateImageLink(string link) => 
        _imageLinkRegex.IsMatch(link);
    
    public static IEnumerable<string> FindLinks(string content) =>
        _linkRegex.Matches(content).Select(x => x.ToString());

}