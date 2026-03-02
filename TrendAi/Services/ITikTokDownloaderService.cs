using TrendAi.Models;

namespace TrendAi.Services;

public interface ITikTokDownloaderService
{
    Task<string?> DownloadVideoAsync(TikTokVideo video);
    Task<List<DownloadResult>> DownloadMultipleAsync(List<TikTokVideo> videos, int maxCount = 5);
    string GetDownloadsFolder();
}

public class DownloadResult
{
    public TikTokVideo Video { get; set; } = null!;
    public string? FilePath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
