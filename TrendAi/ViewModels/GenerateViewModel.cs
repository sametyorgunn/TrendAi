using TrendAi.Models;

namespace TrendAi.ViewModels;

public class GenerateViewModel
{
    public List<AiVideoSuggestion> Suggestions { get; set; } = [];
    public TrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
    public int IdeaCount { get; set; } = 5;
}
