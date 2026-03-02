using TrendAi.Models;

namespace TrendAi.ViewModels;

public class TikTokGenerateViewModel
{
    public List<AiVideoSuggestion> Suggestions { get; set; } = [];
    public TikTokTrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
    public int IdeaCount { get; set; } = 5;
}
