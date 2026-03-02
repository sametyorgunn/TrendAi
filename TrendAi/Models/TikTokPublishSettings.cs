namespace TrendAi.Models;

public class TikTokPublishSettings
{
    /// <summary>
    /// TikTok Developer Portal'dan alınan Client Key
    /// https://developers.tiktok.com/ adresinden oluşturulur
    /// </summary>
    public string ClientKey { get; set; } = string.Empty;

    /// <summary>
    /// TikTok Developer Portal'dan alınan Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth callback URL'si (örn: https://localhost:7xxx/tiktok/callback)
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// OAuth sonrası alınan Access Token (oturum açıldıktan sonra kaydedilir)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Otomatik paylaşım açık mı?
    /// </summary>
    public bool AutoPublishEnabled { get; set; } = false;

    /// <summary>
    /// Paylaşımda kullanılacak varsayılan privacy level.
    /// PUBLIC_TO_EVERYONE, MUTUAL_FOLLOW_FRIENDS, FOLLOWER_OF_CREATOR, SELF_ONLY
    /// </summary>
    public string PrivacyLevel { get; set; } = "PUBLIC_TO_EVERYONE";
}
