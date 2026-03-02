using TrendAi.Models;

namespace TrendAi.Services;

public class TikTokDownloaderService : ITikTokDownloaderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TikTokDownloaderService> _logger;
    private readonly IWebHostEnvironment _env;

    public TikTokDownloaderService(
        HttpClient httpClient,
        ILogger<TikTokDownloaderService> logger,
        IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _logger = logger;
        _env = env;
    }

    public string GetDownloadsFolder()
    {
        var folder = Path.Combine(_env.ContentRootPath, "Downloads", "TikTok");
        Directory.CreateDirectory(folder);
        return folder;
    }

    public async Task<string?> DownloadVideoAsync(TikTokVideo video)
    {
        if (string.IsNullOrEmpty(video.DownloadUrl))
        {
            _logger.LogWarning("Video {Id} için indirme URL'si bulunamadı", video.VideoId);
            return null;
        }

        try
        {
            var folder = GetDownloadsFolder();
            var safeAuthor = SanitizeFileName(video.AuthorName);
            var fileName = $"{safeAuthor}_{video.VideoId}.mp4";
            var filePath = Path.Combine(folder, fileName);

            if (File.Exists(filePath))
            {
                _logger.LogInformation("Video zaten indirilmiş: {Path}", filePath);
                return filePath;
            }

            _logger.LogInformation("Video indiriliyor: {Id} - {Url}", video.VideoId, video.DownloadUrl[..Math.Min(80, video.DownloadUrl.Length)]);

            using var response = await _httpClient.GetAsync(video.DownloadUrl);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            if (bytes.Length < 1000)
            {
                _logger.LogWarning("İndirilen dosya çok küçük ({Size} bytes), geçersiz olabilir", bytes.Length);
                return null;
            }

            await File.WriteAllBytesAsync(filePath, bytes);

            _logger.LogInformation("Video indirildi: {Path} ({Size:F1} MB)", filePath, bytes.Length / 1_048_576.0);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video indirme hatası: {Id}", video.VideoId);
            return null;
        }
    }

    public async Task<List<DownloadResult>> DownloadMultipleAsync(List<TikTokVideo> videos, int maxCount = 5)
    {
        var results = new List<DownloadResult>();
        var toDownload = videos.Take(maxCount).ToList();

        foreach (var video in toDownload)
        {
            var result = new DownloadResult { Video = video };
            try
            {
                result.FilePath = await DownloadVideoAsync(video);
                result.Success = result.FilePath is not null;
                if (!result.Success)
                    result.ErrorMessage = "İndirme URL'si geçersiz veya dosya çok küçük.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            results.Add(result);
        }

        _logger.LogInformation("{Success}/{Total} video başarıyla indirildi",
            results.Count(r => r.Success), results.Count);

        return results;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "unknown" : sanitized;
    }
}
