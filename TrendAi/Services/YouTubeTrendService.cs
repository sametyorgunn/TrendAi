using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Options;
using TrendAi.Models;

namespace TrendAi.Services;

public class YouTubeTrendService : IYouTubeTrendService
{
    private readonly YouTubeService _youtubeService;
    private readonly YouTubeApiSettings _settings;
    private readonly ILogger<YouTubeTrendService> _logger;

    public YouTubeTrendService(IOptions<YouTubeApiSettings> settings, ILogger<YouTubeTrendService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = _settings.ApiKey,
            ApplicationName = "TrendAi"
        });
    }

    public async Task<Dictionary<string, string>> GetVideoCategoriesAsync(string? regionCode = null)
    {
        var region = regionCode ?? _settings.RegionCode;
        var categories = new Dictionary<string, string>();

        try
        {
            var request = _youtubeService.VideoCategories.List("snippet");
            request.RegionCode = region;

            var response = await request.ExecuteAsync();

            foreach (var category in response.Items)
            {
                categories[category.Id] = category.Snippet.Title;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube kategori bilgileri alınırken hata oluştu");
        }

        return categories;
    }

    public async Task<List<TrendingVideo>> GetTrendingVideosAsync(string? regionCode = null)
    {
        var region = regionCode ?? _settings.RegionCode;
        var videos = new List<TrendingVideo>();

        try
        {
            var categories = await GetVideoCategoriesAsync(region);

            var request = _youtubeService.Videos.List("snippet,statistics,contentDetails");
            request.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
            request.RegionCode = region;
            request.MaxResults = _settings.MaxResults;

            var response = await request.ExecuteAsync();

            foreach (var item in response.Items)
            {
                var video = new TrendingVideo
                {
                    VideoId = item.Id,
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    ThumbnailUrl = item.Snippet.Thumbnails?.High?.Url
                                ?? item.Snippet.Thumbnails?.Medium?.Url
                                ?? item.Snippet.Thumbnails?.Default__?.Url
                                ?? string.Empty,
                    CategoryId = item.Snippet.CategoryId,
                    CategoryName = categories.TryGetValue(item.Snippet.CategoryId, out var name) ? name : "Bilinmiyor",
                    ViewCount = (long)(item.Statistics?.ViewCount ?? 0),
                    LikeCount = (long)(item.Statistics?.LikeCount ?? 0),
                    CommentCount = (long)(item.Statistics?.CommentCount ?? 0),
                    PublishedAt = item.Snippet.PublishedAtDateTimeOffset?.DateTime ?? DateTime.MinValue,
                    Duration = ParseDuration(item.ContentDetails?.Duration),
                    Tags = item.Snippet.Tags?.ToList() ?? []
                };

                videos.Add(video);
            }

            _logger.LogInformation("{Count} trend video başarıyla alındı ({Region})", videos.Count, region);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trend videolar alınırken hata oluştu");
        }

        return videos;
    }

    private static string ParseDuration(string? isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration))
            return "0:00";

        try
        {
            var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            return duration.Hours > 0
                ? $"{duration.Hours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
                : $"{duration.Minutes}:{duration.Seconds:D2}";
        }
        catch
        {
            return "0:00";
        }
    }
}
