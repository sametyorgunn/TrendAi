namespace TrendAi.Models;

public class TrendingVideo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Duration { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];

    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";

    public string FormattedViews => ViewCount switch
    {
        >= 1_000_000_000 => $"{ViewCount / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{ViewCount / 1_000_000.0:F1}M",
        >= 1_000 => $"{ViewCount / 1_000.0:F1}K",
        _ => ViewCount.ToString()
    };
}
