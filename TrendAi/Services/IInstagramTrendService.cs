using TrendAi.Models;

namespace TrendAi.Services;

public interface IInstagramTrendService
{
    Task<List<InstagramPost>> GetTrendingPostsAsync(string hashtag = "reels");
}
