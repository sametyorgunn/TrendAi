namespace TrendAi.Services;

public interface ITikTokPublishService
{
    string GetAuthorizationUrl();
    Task<bool> ExchangeCodeForTokenAsync(string code);
    Task<PublishResult> PublishVideoAsync(string videoFilePath, string description, List<string>? hashtags = null);
    bool IsAuthenticated { get; }
}

public class PublishResult
{
    public bool Success { get; set; }
    public string? PublishId { get; set; }
    public string? ErrorMessage { get; set; }
}
