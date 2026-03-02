using TrendVideoAi.Models;

namespace TrendVideoAi.Services;

public interface IYouTubeTrendService
{
    Task<List<TrendingVideo>> GetTrendingVideosAsync(string? regionCode = null);
    Task<Dictionary<string, string>> GetVideoCategoriesAsync(string? regionCode = null);
}
