namespace TrendAi.Models;

public class TikTokVideo
{
    public string VideoId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorNickname { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long PlayCount { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public long ShareCount { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Hashtags { get; set; } = [];
    public string MusicTitle { get; set; } = string.Empty;

    public string TikTokUrl => $"https://www.tiktok.com/@{AuthorName}/video/{VideoId}";

    public string FormattedPlays => PlayCount switch
    {
        >= 1_000_000_000 => $"{PlayCount / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{PlayCount / 1_000_000.0:F1}M",
        >= 1_000 => $"{PlayCount / 1_000.0:F1}K",
        _ => PlayCount.ToString()
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
