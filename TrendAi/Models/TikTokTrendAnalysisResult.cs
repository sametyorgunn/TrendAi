namespace TrendAi.Models;

public class TikTokTrendAnalysisResult
{
    public List<HashtagTrend> TopHashtags { get; set; } = [];
    public List<MusicTrend> TopMusic { get; set; } = [];
    public List<TikTokVideo> AllVideos { get; set; } = [];
    public int TotalVideosAnalyzed { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string Region { get; set; } = string.Empty;
    public long TotalPlays { get; set; }
    public long TotalLikes { get; set; }
    public double AvgEngagementRate { get; set; }
}

public class HashtagTrend
{
    public string Tag { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public long TotalPlays { get; set; }
    public long TotalLikes { get; set; }
    public double EngagementRate { get; set; }

    public string FormattedPlays => TotalPlays switch
    {
        >= 1_000_000_000 => $"{TotalPlays / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{TotalPlays / 1_000_000.0:F1}M",
        >= 1_000 => $"{TotalPlays / 1_000.0:F1}K",
        _ => TotalPlays.ToString()
    };
}

public class MusicTrend
{
    public string Title { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public long TotalPlays { get; set; }
}
