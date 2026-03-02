using TrendAi.Models;

namespace TrendAi.Services;

public interface ITikTokTrendService
{
    Task<List<TikTokVideo>> GetTrendingVideosAsync(string? region = null);
}
