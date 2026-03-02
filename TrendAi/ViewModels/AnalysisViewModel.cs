using TrendAi.Models;

namespace TrendAi.ViewModels;

public class AnalysisViewModel
{
    public TrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
}
