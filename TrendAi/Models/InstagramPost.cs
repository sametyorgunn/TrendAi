namespace TrendAi.Models;

public class InstagramPost
{
    public string PostId { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorFullName { get; set; } = string.Empty;
    public string AuthorProfilePicUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public double Duration { get; set; }
    public DateTime TakenAt { get; set; }
    public List<string> Hashtags { get; set; } = [];
    public string MusicTitle { get; set; } = string.Empty;
    public string PostType { get; set; } = "Reel";

    public string InstagramUrl => $"https://www.instagram.com/reel/{PostId}/";

    public string FormattedViews => ViewCount switch
    {
        >= 1_000_000_000 => $"{ViewCount / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{ViewCount / 1_000_000.0:F1}M",
        >= 1_000 => $"{ViewCount / 1_000.0:F1}K",
        _ => ViewCount.ToString()
    };

    public string FormattedLikes => LikeCount switch
    {
        >= 1_000_000_000 => $"{LikeCount / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{LikeCount / 1_000_000.0:F1}M",
        >= 1_000 => $"{LikeCount / 1_000.0:F1}K",
        _ => LikeCount.ToString()
    };

    public string FormattedDuration
    {
        get
        {
            var ts = TimeSpan.FromSeconds(Duration);
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
}
