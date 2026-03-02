namespace TrendVideoAi.Models;

public class TrendAnalysisResult
{
    public List<VideoCategory> Categories { get; set; } = [];
    public List<TrendingVideo> AllVideos { get; set; } = [];
    public int TotalVideosAnalyzed { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string Region { get; set; } = string.Empty;
    public List<TagTrend> TopTags { get; set; } = [];
    public VideoCategory? TopCategory => Categories.OrderByDescending(c => c.TrendScore).FirstOrDefault();
}

public class TagTrend
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalViews { get; set; }
}
