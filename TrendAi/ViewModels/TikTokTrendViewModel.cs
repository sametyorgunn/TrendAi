using TrendAi.Models;

namespace TrendAi.ViewModels;

public class TikTokTrendViewModel
{
    public List<TikTokVideo> Videos { get; set; } = [];
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
}
