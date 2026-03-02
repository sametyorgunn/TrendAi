namespace TrendAi.Models;

public class YouTubeApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string RegionCode { get; set; } = "TR";
    public int MaxResults { get; set; } = 50;
}

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public class TikTokApiSettings
{
    public string RapidApiKey { get; set; } = string.Empty;
    public string RapidApiHost { get; set; } = "tiktok-scraper7.p.rapidapi.com";
    public string Region { get; set; } = "TR";
    public int MaxResults { get; set; } = 30;
}
