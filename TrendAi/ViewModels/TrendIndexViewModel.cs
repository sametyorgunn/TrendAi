using TrendAi.Models;

namespace TrendAi.ViewModels;

public class TrendIndexViewModel
{
    public List<TrendingVideo> Videos { get; set; } = [];
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
}
