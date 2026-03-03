using TrendAi.Models;

namespace TrendAi.ViewModels;

public class InstagramTrendViewModel
{
    public List<InstagramPost> Posts { get; set; } = [];
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string Hashtag { get; set; } = "reels";
}
