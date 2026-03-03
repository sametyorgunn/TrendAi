using System.Text.Json;
using Microsoft.Extensions.Options;
using TrendAi.Models;

namespace TrendAi.Services;

public class InstagramTrendService : IInstagramTrendService
{
    private readonly HttpClient _httpClient;
    private readonly InstagramApiSettings _settings;
    private readonly ILogger<InstagramTrendService> _logger;

    public InstagramTrendService(
        HttpClient httpClient,
        IOptions<InstagramApiSettings> settings,
        ILogger<InstagramTrendService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<List<InstagramPost>> GetTrendingPostsAsync(string hashtag = "reels")
    {
        var posts = new List<InstagramPost>();

        try
        {
            var url = $"https://{_settings.RapidApiHost}/v1/hashtag_posts?hashtag={Uri.EscapeDataString(hashtag)}&count={_settings.MaxResults}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-rapidapi-key", _settings.RapidApiKey);
            request.Headers.Add("x-rapidapi-host", _settings.RapidApiHost);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Instagram API hatası: {Status} - {Content}", response.StatusCode, content);
                return posts;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var items = ExtractItems(root);

            foreach (var item in items)
            {
                try
                {
                    var post = ParsePost(item);
                    if (post is not null)
                        posts.Add(post);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Instagram post parse hatası");
                }
            }

            _logger.LogInformation("{Count} Instagram trend post alındı (#{Hashtag})", posts.Count, hashtag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Instagram trend postları alınırken hata oluştu");
        }

        return posts.OrderByDescending(p => p.ViewCount > 0 ? p.ViewCount : p.LikeCount).ToList();
    }

    private static IEnumerable<JsonElement> ExtractItems(JsonElement root)
    {
        // Format 1: { "data": { "items": [...] } }
        if (root.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("items", out var items1) && items1.ValueKind == JsonValueKind.Array)
                return items1.EnumerateArray();

            // Format 2: { "data": { "sections": [ { "layout_content": { "medias": [...] } } ] } }
            if (data.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            {
                var medias = new List<JsonElement>();
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.TryGetProperty("layout_content", out var lc) &&
                        lc.TryGetProperty("medias", out var mediaArr) &&
                        mediaArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in mediaArr.EnumerateArray())
                        {
                            if (m.TryGetProperty("media", out var media))
                                medias.Add(media);
                            else
                                medias.Add(m);
                        }
                    }
                }
                return medias;
            }

            // Format 3: { "data": [...] }
            if (data.ValueKind == JsonValueKind.Array)
                return data.EnumerateArray();
        }

        // Format 4: { "items": [...] }
        if (root.TryGetProperty("items", out var items2) && items2.ValueKind == JsonValueKind.Array)
            return items2.EnumerateArray();

        return [];
    }

    private static InstagramPost? ParsePost(JsonElement item)
    {
        var post = new InstagramPost();

        post.PostId = GetString(item, "id") ?? GetString(item, "pk") ?? GetString(item, "code") ?? string.Empty;
        if (string.IsNullOrEmpty(post.PostId))
            return null;

        // Caption
        if (item.TryGetProperty("caption", out var caption))
        {
            post.Caption = GetString(caption, "text") ?? GetString(item, "caption") ?? string.Empty;
        }
        else
        {
            post.Caption = GetString(item, "caption") ?? GetString(item, "text") ?? string.Empty;
        }

        // Yazar
        if (item.TryGetProperty("user", out var user))
        {
            post.AuthorUsername = GetString(user, "username") ?? string.Empty;
            post.AuthorFullName = GetString(user, "full_name") ?? GetString(user, "fullName") ?? post.AuthorUsername;
            post.AuthorProfilePicUrl = GetString(user, "profile_pic_url") ?? GetString(user, "profilePicUrl") ?? string.Empty;
        }

        // Thumbnail / Kapak
        if (item.TryGetProperty("image_versions2", out var imgVer) &&
            imgVer.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array)
        {
            var first = candidates.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object)
                post.ThumbnailUrl = GetString(first, "url") ?? string.Empty;
        }
        else
        {
            post.ThumbnailUrl = GetString(item, "thumbnail_url") ?? GetString(item, "display_url") ?? GetString(item, "thumbnail") ?? string.Empty;
        }

        // Video URL
        post.VideoUrl = GetString(item, "video_url") ?? GetString(item, "videoUrl") ?? string.Empty;

        // Süre
        if (item.TryGetProperty("video_duration", out var durEl))
        {
            post.Duration = durEl.ValueKind == JsonValueKind.Number ? durEl.GetDouble() : 0;
        }

        // İstatistikler
        post.ViewCount = GetLong(item, "play_count") ?? GetLong(item, "view_count") ?? GetLong(item, "video_view_count") ?? 0;
        post.LikeCount = GetLong(item, "like_count") ?? GetLong(item, "likeCount") ?? 0;
        post.CommentCount = GetLong(item, "comment_count") ?? GetLong(item, "commentCount") ?? 0;

        // Tarih
        if (GetLong(item, "taken_at") is long ts)
            post.TakenAt = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;

        // Medya türü (1=foto, 2=video/reel, 8=carousel)
        var mediaType = GetInt(item, "media_type");
        post.PostType = mediaType == 2 ? "Reel" : mediaType == 8 ? "Carousel" : "Post";

        // Hashtag'leri caption'dan çıkar
        if (!string.IsNullOrEmpty(post.Caption))
        {
            foreach (var word in post.Caption.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.StartsWith('#') && word.Length > 1)
                {
                    var tag = word[1..].TrimEnd('.', ',', '!', '?');
                    if (!string.IsNullOrEmpty(tag))
                        post.Hashtags.Add(tag.ToLowerInvariant());
                }
            }
        }

        // Müzik
        if (item.TryGetProperty("music_metadata", out var musicMeta))
        {
            if (musicMeta.TryGetProperty("music_info", out var musicInfo))
                post.MusicTitle = GetString(musicInfo, "music_asset_info") is string _ ? string.Empty : GetString(musicInfo, "title") ?? string.Empty;

            if (string.IsNullOrEmpty(post.MusicTitle) && musicMeta.TryGetProperty("original_sound_info", out var soundInfo))
                post.MusicTitle = GetString(soundInfo, "original_audio_title") ?? string.Empty;
        }

        if (string.IsNullOrEmpty(post.MusicTitle))
        {
            if (item.TryGetProperty("clips_metadata", out var clipsMeta) &&
                clipsMeta.TryGetProperty("music_info", out var cmi))
            {
                post.MusicTitle = GetString(cmi, "music_canonical_id") is null
                    ? (GetString(cmi, "music_asset_info") ?? string.Empty)
                    : string.Empty;

                if (cmi.TryGetProperty("music_asset_info", out var mai))
                    post.MusicTitle = GetString(mai, "title") ?? string.Empty;
            }
        }

        return post;
    }

    private static string? GetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private static long? GetLong(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val))
            return null;

        return val.ValueKind switch
        {
            JsonValueKind.Number => val.TryGetInt64(out var n) ? n : (long?)null,
            JsonValueKind.String when long.TryParse(val.GetString(), out var n) => n,
            _ => null
        };
    }

    private static int GetInt(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val))
            return 0;

        return val.ValueKind switch
        {
            JsonValueKind.Number => val.GetInt32(),
            JsonValueKind.String when int.TryParse(val.GetString(), out var n) => n,
            _ => 0
        };
    }
}
