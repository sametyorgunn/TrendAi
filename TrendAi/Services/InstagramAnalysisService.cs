using TrendAi.Models;

namespace TrendAi.Services;

public class InstagramAnalysisService : IInstagramAnalysisService
{
    public InstagramTrendAnalysisResult Analyze(List<InstagramPost> posts, string category)
    {
        var result = new InstagramTrendAnalysisResult
        {
            AllPosts = posts,
            TotalPostsAnalyzed = posts.Count,
            Category = category,
            AnalyzedAt = DateTime.UtcNow,
            TotalViews = posts.Sum(p => p.ViewCount),
            TotalLikes = posts.Sum(p => p.LikeCount)
        };

        if (result.TotalViews > 0)
        {
            var totalEngagement = posts.Sum(p => p.LikeCount + p.CommentCount);
            result.AvgEngagementRate = (double)totalEngagement / result.TotalViews * 100;
        }
        else if (result.TotalLikes > 0)
        {
            var totalEngagement = posts.Sum(p => p.LikeCount + p.CommentCount);
            result.AvgEngagementRate = (double)totalEngagement / posts.Count;
        }

        // Hashtag analizi
        result.TopHashtags = posts
            .SelectMany(p => p.Hashtags.Select(h => new
            {
                Tag = h.ToLowerInvariant(),
                p.ViewCount,
                p.LikeCount,
                p.CommentCount
            }))
            .GroupBy(x => x.Tag)
            .Select(g =>
            {
                var totalViews = g.Sum(x => x.ViewCount);
                var totalLikes = g.Sum(x => x.LikeCount);
                var totalEngagement = g.Sum(x => x.LikeCount + x.CommentCount);
                return new InstagramHashtagTrend
                {
                    Tag = g.Key,
                    PostCount = g.Count(),
                    TotalViews = totalViews,
                    TotalLikes = totalLikes,
                    EngagementRate = totalViews > 0
                        ? (double)totalEngagement / totalViews * 100
                        : totalLikes > 0 ? (double)totalEngagement / g.Count() : 0
                };
            })
            .OrderByDescending(h => h.PostCount)
            .ThenByDescending(h => h.TotalViews)
            .Take(30)
            .ToList();

        // Müzik analizi
        result.TopMusic = posts
            .Where(p => !string.IsNullOrEmpty(p.MusicTitle))
            .GroupBy(p => p.MusicTitle)
            .Select(g => new InstagramMusicTrend
            {
                Title = g.Key,
                PostCount = g.Count(),
                TotalViews = g.Sum(p => p.ViewCount)
            })
            .OrderByDescending(m => m.PostCount)
            .ThenByDescending(m => m.TotalViews)
            .Take(15)
            .ToList();

        return result;
    }
}
