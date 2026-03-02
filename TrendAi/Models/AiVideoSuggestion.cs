namespace TrendAi.Models;

public class AiVideoSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string ThumbnailIdea { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string EstimatedDuration { get; set; } = string.Empty;
    public string WhyItWorks { get; set; } = string.Empty;
}
