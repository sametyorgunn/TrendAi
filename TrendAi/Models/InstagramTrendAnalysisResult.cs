namespace TrendAi.Models;

public class InstagramTrendAnalysisResult
{
    public List<InstagramHashtagTrend> TopHashtags { get; set; } = [];
    public List<InstagramMusicTrend> TopMusic { get; set; } = [];
    public List<InstagramPost> AllPosts { get; set; } = [];
    public int TotalPostsAnalyzed { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;
    public long TotalViews { get; set; }
    public long TotalLikes { get; set; }
    public double AvgEngagementRate { get; set; }
}

public class InstagramHashtagTrend
{
    public string Tag { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public long TotalViews { get; set; }
    public long TotalLikes { get; set; }
    public double EngagementRate { get; set; }

    public string FormattedViews => TotalViews switch
    {
        >= 1_000_000_000 => $"{TotalViews / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{TotalViews / 1_000_000.0:F1}M",
        >= 1_000 => $"{TotalViews / 1_000.0:F1}K",
        _ => TotalViews.ToString()
    };
}

public class InstagramMusicTrend
{
    public string Title { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public long TotalViews { get; set; }
}
