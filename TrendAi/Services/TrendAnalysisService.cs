using TrendAi.Models;

namespace TrendAi.Services;

public class TrendAnalysisService : ITrendAnalysisService
{
    public TrendAnalysisResult AnalyzeTrends(List<TrendingVideo> videos, string region)
    {
        var result = new TrendAnalysisResult
        {
            AllVideos = videos,
            TotalVideosAnalyzed = videos.Count,
            Region = region,
            AnalyzedAt = DateTime.UtcNow
        };

        var grouped = videos.GroupBy(v => new { v.CategoryId, v.CategoryName });

        foreach (var group in grouped)
        {
            var categoryVideos = group.ToList();
            var totalViews = categoryVideos.Sum(v => v.ViewCount);

            var allTags = categoryVideos
                .SelectMany(v => v.Tags)
                .GroupBy(t => t.ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            var trendScore = CalculateTrendScore(categoryVideos, videos.Count);

            result.Categories.Add(new VideoCategory
            {
                Id = group.Key.CategoryId,
                Name = group.Key.CategoryName,
                VideoCount = categoryVideos.Count,
                TotalViews = totalViews,
                AverageViews = categoryVideos.Count > 0 ? totalViews / categoryVideos.Count : 0,
                TrendScore = trendScore,
                TopTags = allTags,
                Videos = categoryVideos.OrderByDescending(v => v.ViewCount).ToList()
            });
        }

        result.Categories = result.Categories.OrderByDescending(c => c.TrendScore).ToList();

        result.TopTags = videos
            .SelectMany(v => v.Tags.Select(t => new { Tag = t.ToLowerInvariant(), v.ViewCount }))
            .GroupBy(x => x.Tag)
            .Select(g => new TagTrend
            {
                Tag = g.Key,
                Count = g.Count(),
                TotalViews = g.Sum(x => x.ViewCount)
            })
            .OrderByDescending(t => t.Count)
            .ThenByDescending(t => t.TotalViews)
            .Take(30)
            .ToList();

        return result;
    }

    private static double CalculateTrendScore(List<TrendingVideo> categoryVideos, int totalVideoCount)
    {
        if (totalVideoCount == 0)
            return 0;

        var categoryRatio = (double)categoryVideos.Count / totalVideoCount * 100;
        var avgViews = categoryVideos.Average(v => (double)v.ViewCount);
        var avgLikes = categoryVideos.Average(v => (double)v.LikeCount);
        var avgComments = categoryVideos.Average(v => (double)v.CommentCount);

        var normalizedViews = Math.Log10(avgViews + 1) * 10;
        var normalizedEngagement = Math.Log10(avgLikes + avgComments + 1) * 5;

        return categoryRatio * 0.4 + normalizedViews * 0.35 + normalizedEngagement * 0.25;
    }
}
