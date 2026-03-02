using System.Text.Json;
using Microsoft.Extensions.Options;
using TrendAi.Models;

namespace TrendAi.Services;

public class TikTokTrendService : ITikTokTrendService
{
    private readonly HttpClient _httpClient;
    private readonly TikTokApiSettings _settings;
    private readonly ILogger<TikTokTrendService> _logger;

    public TikTokTrendService(
        HttpClient httpClient,
        IOptions<TikTokApiSettings> settings,
        ILogger<TikTokTrendService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<List<TikTokVideo>> GetTrendingVideosAsync(string? region = null)
    {
        var videos = new List<TikTokVideo>();
        var targetRegion = region ?? _settings.Region;

        try
        {
            var url = $"https://{_settings.RapidApiHost}/feed/list?region={targetRegion}&count={_settings.MaxResults}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-rapidapi-key", _settings.RapidApiKey);
            request.Headers.Add("x-rapidapi-host", _settings.RapidApiHost);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TikTok API hatası: {Status} - {Content}", response.StatusCode, content);
                return videos;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // /feed/list yanıt formatı: { "code": 0, "data": [...] }
            JsonElement videoList;
            if (root.TryGetProperty("data", out var dataEl))
            {
                if (dataEl.ValueKind == JsonValueKind.Array)
                    videoList = dataEl;
                else if (dataEl.TryGetProperty("videos", out videoList) ||
                         dataEl.TryGetProperty("aweme_list", out videoList))
                { }
                else
                {
                    _logger.LogWarning("TikTok API yanıtında video listesi bulunamadı");
                    return videos;
                }
            }
            else
            {
                _logger.LogWarning("TikTok API yanıtında 'data' bulunamadı");
                return videos;
            }

            foreach (var item in videoList.EnumerateArray())
            {
                try
                {
                    var video = ParseVideoFromJson(item);
                    if (video is not null)
                        videos.Add(video);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TikTok video parse hatası");
                }
            }

            _logger.LogInformation("{Count} TikTok trend video alındı ({Region})", videos.Count, targetRegion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok trend videoları alınırken hata oluştu");
        }

        return videos;
    }

    private static TikTokVideo? ParseVideoFromJson(JsonElement item)
    {
        var video = new TikTokVideo();

        // Video ID
        video.VideoId = GetString(item, "aweme_id") ?? GetString(item, "id") ?? GetString(item, "video_id") ?? string.Empty;
        if (string.IsNullOrEmpty(video.VideoId))
            return null;

        // Açıklama — tiktok-scraper7 "title" kullanıyor
        video.Description = GetString(item, "title") ?? GetString(item, "desc") ?? GetString(item, "description") ?? string.Empty;

        // Yazar bilgileri
        if (item.TryGetProperty("author", out var author))
        {
            video.AuthorName = GetString(author, "unique_id") ?? GetString(author, "uniqueId") ?? string.Empty;
            video.AuthorNickname = GetString(author, "nickname") ?? video.AuthorName;
            video.AuthorAvatar = GetString(author, "avatar") ?? GetString(author, "avatarThumb")
                              ?? GetString(author, "avatar_thumb") ?? string.Empty;
        }

        // Kapak görseli — tiktok-scraper7 "cover" doğrudan item üzerinde
        video.CoverUrl = GetString(item, "cover") ?? GetString(item, "ai_dynamic_cover")
                      ?? GetString(item, "origin_cover") ?? string.Empty;

        // Video süresi — tiktok-scraper7 "duration" doğrudan item üzerinde
        video.Duration = GetInt(item, "duration");

        // İndirme URL'si — "play" filigransız, "wmplay" filigranlı
        video.DownloadUrl = GetString(item, "play") ?? GetString(item, "wmplay") ?? string.Empty;

        // İstatistikler — tiktok-scraper7 düz property olarak veriyor
        video.PlayCount = GetLong(item, "play_count") ?? GetLong(item, "playCount") ?? 0;
        video.LikeCount = GetLong(item, "digg_count") ?? GetLong(item, "diggCount") ?? GetLong(item, "like_count") ?? 0;
        video.CommentCount = GetLong(item, "comment_count") ?? GetLong(item, "commentCount") ?? 0;
        video.ShareCount = GetLong(item, "share_count") ?? GetLong(item, "shareCount") ?? 0;

        // stats nesnesi varsa (farklı API formatı)
        if (video.PlayCount == 0 && item.TryGetProperty("stats", out var stats))
        {
            video.PlayCount = GetLong(stats, "playCount") ?? GetLong(stats, "play_count") ?? 0;
            video.LikeCount = GetLong(stats, "diggCount") ?? GetLong(stats, "likeCount") ?? 0;
            video.CommentCount = GetLong(stats, "commentCount") ?? 0;
            video.ShareCount = GetLong(stats, "shareCount") ?? 0;
        }

        // Oluşturulma tarihi
        if (GetLong(item, "create_time") is long ts)
            video.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
        else if (GetLong(item, "createTime") is long ts2)
            video.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(ts2).DateTime;

        // Hashtag'leri description'dan çıkar
        if (!string.IsNullOrEmpty(video.Description))
        {
            foreach (var word in video.Description.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.StartsWith('#') && word.Length > 1)
                {
                    var tag = word[1..].TrimEnd('.', ',', '!', '?');
                    if (!string.IsNullOrEmpty(tag))
                        video.Hashtags.Add(tag);
                }
            }
        }

        // Müzik — tiktok-scraper7 "music_info" kullanıyor
        if (item.TryGetProperty("music_info", out var musicInfo))
        {
            video.MusicTitle = GetString(musicInfo, "title") ?? string.Empty;
        }
        else if (item.TryGetProperty("music", out var music))
        {
            video.MusicTitle = GetString(music, "title") ?? string.Empty;
        }

        return video;
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
            JsonValueKind.Number => val.GetInt64(),
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
