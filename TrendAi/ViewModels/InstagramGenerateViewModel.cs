using TrendAi.Models;

namespace TrendAi.ViewModels;

public class InstagramGenerateViewModel
{
    public List<AiVideoSuggestion> Suggestions { get; set; } = [];
    public InstagramTrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string Hashtag { get; set; } = "reels";
    public int IdeaCount { get; set; } = 5;
}
