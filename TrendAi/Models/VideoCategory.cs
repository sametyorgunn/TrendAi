namespace TrendAi.Models;

public class VideoCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public long TotalViews { get; set; }
    public long AverageViews { get; set; }
    public double TrendScore { get; set; }
    public List<string> TopTags { get; set; } = [];
    public List<TrendingVideo> Videos { get; set; } = [];

    public string FormattedTotalViews => TotalViews switch
    {
        >= 1_000_000_000 => $"{TotalViews / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{TotalViews / 1_000_000.0:F1}M",
        >= 1_000 => $"{TotalViews / 1_000.0:F1}K",
        _ => TotalViews.ToString()
    };
}
