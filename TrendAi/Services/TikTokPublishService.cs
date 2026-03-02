using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrendAi.Models;

namespace TrendAi.Services;

public class TikTokPublishService : ITikTokPublishService
{
    private readonly HttpClient _httpClient;
    private readonly TikTokPublishSettings _settings;
    private readonly ILogger<TikTokPublishService> _logger;
    private string _accessToken;

    public TikTokPublishService(
        HttpClient httpClient,
        IOptions<TikTokPublishSettings> settings,
        ILogger<TikTokPublishService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _accessToken = _settings.AccessToken;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public string GetAuthorizationUrl()
    {
        var scopes = "user.info.basic,video.publish,video.upload";
        return $"https://www.tiktok.com/v2/auth/authorize/" +
               $"?client_key={_settings.ClientKey}" +
               $"&scope={scopes}" +
               $"&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
               $"&state=trendai";
    }

    public async Task<bool> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            var requestBody = new Dictionary<string, string>
            {
                ["client_key"] = _settings.ClientKey,
                ["client_secret"] = _settings.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = _settings.RedirectUri
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TikTok OAuth token hatası: {Response}", responseString);
                return false;
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var tokenEl))
            {
                _accessToken = tokenEl.GetString() ?? string.Empty;
                _logger.LogInformation("TikTok OAuth başarılı, token alındı");
                return true;
            }

            _logger.LogError("TikTok OAuth yanıtında token bulunamadı: {Response}", responseString);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok OAuth token değişim hatası");
            return false;
        }
    }

    public async Task<PublishResult> PublishVideoAsync(string videoFilePath, string description, List<string>? hashtags = null)
    {
        if (!IsAuthenticated)
            return new PublishResult { Success = false, ErrorMessage = "TikTok hesabına bağlanılmamış. Önce OAuth ile giriş yapın." };

        if (!File.Exists(videoFilePath))
            return new PublishResult { Success = false, ErrorMessage = $"Video dosyası bulunamadı: {videoFilePath}" };

        try
        {
            // Adım 1: Video yükleme başlat (init)
            var fileInfo = new FileInfo(videoFilePath);
            var fullDescription = BuildDescription(description, hashtags);

            var initBody = new
            {
                post_info = new
                {
                    title = fullDescription,
                    privacy_level = _settings.PrivacyLevel,
                    disable_duet = false,
                    disable_comment = false,
                    disable_stitch = false
                },
                source_info = new
                {
                    source = "FILE_UPLOAD",
                    video_size = fileInfo.Length,
                    chunk_size = fileInfo.Length,
                    total_chunk_count = 1
                }
            };

            var initJson = JsonSerializer.Serialize(initBody);
            var initContent = new StringContent(initJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var initResponse = await _httpClient.PostAsync(
                "https://open.tiktokapis.com/v2/post/publish/video/init/", initContent);
            var initResponseString = await initResponse.Content.ReadAsStringAsync();

            if (!initResponse.IsSuccessStatusCode)
            {
                _logger.LogError("TikTok video init hatası: {Response}", initResponseString);
                return new PublishResult { Success = false, ErrorMessage = $"Video yükleme başlatılamadı: {initResponseString}" };
            }

            using var initDoc = JsonDocument.Parse(initResponseString);
            var initRoot = initDoc.RootElement;

            if (!initRoot.TryGetProperty("data", out var initData) ||
                !initData.TryGetProperty("publish_id", out var publishIdEl) ||
                !initData.TryGetProperty("upload_url", out var uploadUrlEl))
            {
                return new PublishResult { Success = false, ErrorMessage = "TikTok API'den upload URL alınamadı" };
            }

            var publishId = publishIdEl.GetString()!;
            var uploadUrl = uploadUrlEl.GetString()!;

            // Adım 2: Videoyu yükle
            var videoBytes = await File.ReadAllBytesAsync(videoFilePath);
            var uploadContent = new ByteArrayContent(videoBytes);
            uploadContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            uploadContent.Headers.Add("Content-Range", $"bytes 0-{videoBytes.Length - 1}/{videoBytes.Length}");

            var uploadResponse = await _httpClient.PutAsync(uploadUrl, uploadContent);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var uploadError = await uploadResponse.Content.ReadAsStringAsync();
                _logger.LogError("TikTok video yükleme hatası: {Response}", uploadError);
                return new PublishResult { Success = false, ErrorMessage = $"Video yüklenemedi: {uploadError}" };
            }

            _logger.LogInformation("Video TikTok'a yüklendi. PublishId: {PublishId}", publishId);
            return new PublishResult { Success = true, PublishId = publishId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok video paylaşım hatası");
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static string BuildDescription(string description, List<string>? hashtags)
    {
        var sb = new StringBuilder(description);
        if (hashtags is { Count: > 0 })
        {
            sb.Append(' ');
            foreach (var tag in hashtags)
            {
                var cleanTag = tag.StartsWith('#') ? tag : $"#{tag}";
                sb.Append($"{cleanTag} ");
            }
        }
        return sb.ToString().Trim();
    }
}
