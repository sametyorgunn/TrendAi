using TrendAi.Models;

namespace TrendAi.ViewModels;

public class InstagramAnalysisViewModel
{
    public InstagramTrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string Hashtag { get; set; } = "reels";
}
