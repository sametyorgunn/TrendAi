using TrendAi.Models;

namespace TrendAi.ViewModels;

public class TikTokAnalysisViewModel
{
    public TikTokTrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
}
