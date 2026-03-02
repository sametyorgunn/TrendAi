using TrendAi.Models;

namespace TrendAi.Services;

public class TikTokAnalysisService : ITikTokAnalysisService
{
    public TikTokTrendAnalysisResult Analyze(List<TikTokVideo> videos, string region)
    {
        var result = new TikTokTrendAnalysisResult
        {
            AllVideos = videos,
            TotalVideosAnalyzed = videos.Count,
            Region = region,
            AnalyzedAt = DateTime.UtcNow,
            TotalPlays = videos.Sum(v => v.PlayCount),
            TotalLikes = videos.Sum(v => v.LikeCount)
        };

        // Ortalama engagement rate
        if (result.TotalPlays > 0)
        {
            var totalEngagement = videos.Sum(v => v.LikeCount + v.CommentCount + v.ShareCount);
            result.AvgEngagementRate = (double)totalEngagement / result.TotalPlays * 100;
        }

        // Hashtag analizi
        result.TopHashtags = videos
            .SelectMany(v => v.Hashtags.Select(h => new { Tag = h.ToLowerInvariant(), v.PlayCount, v.LikeCount, v.CommentCount, v.ShareCount }))
            .GroupBy(x => x.Tag)
            .Select(g =>
            {
                var totalPlays = g.Sum(x => x.PlayCount);
                var totalLikes = g.Sum(x => x.LikeCount);
                var totalEngagement = g.Sum(x => x.LikeCount + x.CommentCount + x.ShareCount);
                return new HashtagTrend
                {
                    Tag = g.Key,
                    VideoCount = g.Count(),
                    TotalPlays = totalPlays,
                    TotalLikes = totalLikes,
                    EngagementRate = totalPlays > 0 ? (double)totalEngagement / totalPlays * 100 : 0
                };
            })
            .OrderByDescending(h => h.VideoCount)
            .ThenByDescending(h => h.TotalPlays)
            .Take(30)
            .ToList();

        // Müzik analizi
        result.TopMusic = videos
            .Where(v => !string.IsNullOrEmpty(v.MusicTitle))
            .GroupBy(v => v.MusicTitle)
            .Select(g => new MusicTrend
            {
                Title = g.Key,
                VideoCount = g.Count(),
                TotalPlays = g.Sum(v => v.PlayCount)
            })
            .OrderByDescending(m => m.VideoCount)
            .ThenByDescending(m => m.TotalPlays)
            .Take(15)
            .ToList();

        return result;
    }
}
