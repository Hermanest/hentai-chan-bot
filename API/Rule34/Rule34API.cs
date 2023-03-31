using System.Net.Http.Headers;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using HentaiChanBot.Utils;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HentaiChanBot.API.Rule34;

[PublicAPI]
internal class Rule34Api {
    public const string API_URL = "https://rule34.xxx";
    public const string POSTS_ENDPOINT = "/index.php?page=post&s=list&q=index";
    public const string POST_VIEW_ENDPOINT = "/index.php?page=post&s=view";
    public const string AUTOCOMPLETE_ENDPOINT = "/public/autocomplete.php?";

    public Rule34Api(
        HttpClient? client = null,
        ILogger<Rule34Api>? logger = null) {
        _client = client ?? new();
        _logger = logger;
    }

    private static readonly HtmlParser _htmlParser = new();
    private static readonly ProductInfoHeaderValue _browserHeader = new("Mozilla", "5.0");

    private readonly HttpClient _client;
    private readonly ILogger? _logger;

    public async Task<R34Tag[]?> GetAutocompleteTags(string input) {
        var content = await GetReadAsync(GetAutocompleteUrl(input));
        if (content == null) return null;
        try {
            return JsonConvert.DeserializeObject<R34Tag[]>(content);
        } catch (Exception ex) {
            _logger?.LogError("Failed to deserialize tags:\r\n" + ex);
            return null;
        }
    }
    
    public async Task<int> GetPostsCountAsync(string? tags) {
        var doc = await GetParseAsync(GetPageUrl(tags, 0));
        if (doc == null) return 0;
        var lastLink = doc.All.FirstOrDefault(x => x
            .HasAttributeValue("alt", "last page"))?.GetAttribute("href");
        if (lastLink is null) return GetPostIdsInternal(doc)?.Length ?? 0;
        int.TryParse(lastLink.Remove(0, lastLink.LastIndexOf('=') + 1), out var pid);
        return pid;
    }

    public async Task<int[]?> GetPostIdsAsync(string tags, int pid) {
        var doc = await GetParseAsync(GetPageUrl(tags, pid));
        return doc is null ? null : GetPostIdsInternal(doc);
    }

    public async Task<R34PostData?> GetPostAsync(int id) {
        var doc = await GetParseAsync(GetPostUrl(id));
        if (doc is null) return null;
        var characters = GetElement(doc, "tag-type-character tag");
        var artists = GetElement(doc, "tag-type-artist tag");
        var sampleUrl = doc.All.FirstOrDefault(x =>
            x.HasAttributeValue("id", "image"))?.GetAttribute("src");
        //var tags = GetElement(doc, "tag-type-general tag");
        return new() {
            id = id,
            sampleUrl = sampleUrl,
            artists = artists,
            characters = characters,
            //tags = tags
        };

        static string[]? GetElement(IDocument doc, string className) => doc.All
            .Where(x => x.Attributes.FirstOrDefault()?
                .TextContent == className)
            .Select(x => x
                .Children
                .FirstOrDefault(y => y.Attributes
                    .FirstOrDefault()?.TextContent
                    .Contains("index.php?page=post&s=list&tags=") ?? false)?
                .TextContent)
            .Where(x => x is not null)
            .ToArray()!;
    }

    public string GetPostUrl(int id) {
        return $"{API_URL}{POST_VIEW_ENDPOINT}&id={id}";
    }

    public string GetPageUrl(string? tags, int pid) {
        return $"{API_URL}{POSTS_ENDPOINT}&pid={pid}{(tags is not null ? $"&tags={tags}" : string.Empty)}";
    }

    private string GetAutocompleteUrl(string input) {
        return $"{API_URL}{AUTOCOMPLETE_ENDPOINT}q={input}";
    }

    private int[]? GetPostIdsInternal(IHtmlDocument doc) {
        try {
            return doc.All
                .FirstOrDefault(x => x.HasAttributeValue("class", "image-list"))?.Children
                .Where(x => x.HasAttributeValue("class", "thumb"))
                .Select(x => int.Parse(x.GetAttribute("id")!.Remove(0, 1))).ToArray();
        } catch (Exception ex) {
            _logger?.LogError("Failed to parse posts page:\r\n" + ex);
            return null;
        }
    }

    private async Task<IHtmlDocument?> GetParseAsync(string url) {
        var content = await GetReadAsync(url);
        if (content is null) return null;
        return await _htmlParser.ParseDocumentAsync(content);
    }
    
    private async Task<string?> GetReadAsync(string url) {
        _logger?.LogTrace($"Attempting to request: \"{url}\"");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
            _logger?.LogError("Failed to parse uri!");
            return null;
        }
        var req = new HttpRequestMessage() {
            RequestUri = uri,
            Method = HttpMethod.Get
        };
        req.Headers.UserAgent.Add(_browserHeader);
        var resp = await _client.SendAsync(req);
        var content = await resp.Content.ReadAsStringAsync();
        _logger?.LogTrace("Finished. Content size: " + content.Length);
        return content;
    }
}