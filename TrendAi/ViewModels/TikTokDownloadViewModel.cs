using TrendAi.Models;
using TrendAi.Services;

namespace TrendAi.ViewModels;

public class TikTokDownloadViewModel
{
    public List<TikTokVideo> Videos { get; set; } = [];
    public List<DownloadResult> DownloadResults { get; set; } = [];
    public bool IsLoaded { get; set; }
    public bool IsDownloaded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string RegionCode { get; set; } = "TR";
    public int DownloadCount { get; set; } = 5;
    public bool IsAuthenticated { get; set; }
    public string? AuthUrl { get; set; }
}
