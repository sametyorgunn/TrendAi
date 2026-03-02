using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TrendVideoAi.Models;

namespace TrendVideoAi.Services;

public class AiVideoGeneratorService : IAiVideoGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<AiVideoGeneratorService> _logger;

    public AiVideoGeneratorService(
        HttpClient httpClient,
        IOptions<OpenAiSettings> settings,
        ILogger<AiVideoGeneratorService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<List<AiVideoSuggestion>> GenerateVideoIdeasAsync(TrendAnalysisResult analysis, int count = 5)
    {
        try
        {
            var trendSummary = BuildTrendSummary(analysis);
            var prompt = BuildPrompt(trendSummary, count);

            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = """
                            Sen bir YouTube video içerik stratejisti ve yaratıcı direktörsün. 
                            Trend analizlerine dayanarak viral olma potansiyeli yüksek video fikirleri üretiyorsun.
                            Yanıtlarını her zaman geçerli JSON formatında ver.
                            """
                    },
                    new { role = "user", content = prompt }
                },
                temperature = 0.8,
                max_tokens = 4000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API hatası: {Status} - {Response}", response.StatusCode, responseString);
                return GenerateFallbackSuggestions(analysis, count);
            }

            var result = JsonSerializer.Deserialize<OpenAiResponse>(responseString);
            var messageContent = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(messageContent))
                return GenerateFallbackSuggestions(analysis, count);

            var jsonStart = messageContent.IndexOf('[');
            var jsonEnd = messageContent.LastIndexOf(']');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                messageContent = messageContent[jsonStart..(jsonEnd + 1)];
            }

            var suggestions = JsonSerializer.Deserialize<List<AiVideoSuggestion>>(messageContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return suggestions ?? GenerateFallbackSuggestions(analysis, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI video önerileri oluşturulurken hata oluştu");
            return GenerateFallbackSuggestions(analysis, count);
        }
    }

    private static string BuildTrendSummary(TrendAnalysisResult analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Bölge: {analysis.Region}");
        sb.AppendLine($"Analiz edilen video sayısı: {analysis.TotalVideosAnalyzed}");
        sb.AppendLine();
        sb.AppendLine("=== KATEGORİ ANALİZİ ===");

        foreach (var category in analysis.Categories.Take(5))
        {
            sb.AppendLine($"Kategori: {category.Name}");
            sb.AppendLine($"  Video Sayısı: {category.VideoCount}");
            sb.AppendLine($"  Toplam İzlenme: {category.FormattedTotalViews}");
            sb.AppendLine($"  Trend Skoru: {category.TrendScore:F1}");
            sb.AppendLine($"  Popüler Etiketler: {string.Join(", ", category.TopTags.Take(5))}");

            var topVideo = category.Videos.FirstOrDefault();
            if (topVideo is not null)
                sb.AppendLine($"  En Çok İzlenen: \"{topVideo.Title}\" ({topVideo.FormattedViews} izlenme)");
            sb.AppendLine();
        }

        sb.AppendLine("=== EN POPÜLER ETİKETLER ===");
        foreach (var tag in analysis.TopTags.Take(15))
        {
            sb.AppendLine($"  #{tag.Tag} - {tag.Count} video, toplam {tag.TotalViews:N0} izlenme");
        }

        return sb.ToString();
    }

    private static string BuildPrompt(string trendSummary, int count)
    {
        return $"""
            Aşağıdaki YouTube trend verilerine dayanarak {count} adet viral olma potansiyeli yüksek video fikri üret.

            {trendSummary}

            Her video fikri için şu bilgileri JSON dizisi formatında ver:
            [
              {{
                "title": "Video başlığı (dikkat çekici, tıklanabilir)",
                "description": "Video açıklaması (SEO uyumlu, 2-3 cümle)",
                "category": "Hangi kategoride olacağı",
                "script": "Videonun kısa senaryosu/akış planı (5-10 cümle)",
                "tags": ["etiket1", "etiket2", "etiket3", "etiket4", "etiket5"],
                "thumbnailIdea": "Thumbnail (küçük resim) tasarım önerisi",
                "targetAudience": "Hedef kitle açıklaması",
                "estimatedDuration": "Tahmini video süresi",
                "whyItWorks": "Bu videonun neden tutacağının kısa açıklaması"
              }}
            ]

            SADECE JSON dizisi döndür, başka hiçbir metin ekleme.
            """;
    }

    private static List<AiVideoSuggestion> GenerateFallbackSuggestions(TrendAnalysisResult analysis, int count)
    {
        var suggestions = new List<AiVideoSuggestion>();

        foreach (var category in analysis.Categories.Take(count))
        {
            var topVideo = category.Videos.FirstOrDefault();
            var topTags = category.TopTags.Take(5).ToList();

            suggestions.Add(new AiVideoSuggestion
            {
                Title = $"{category.Name} Kategorisinde Trend İçerik Fikri",
                Description = $"Bu video {category.Name} kategorisindeki güncel trendlere dayanarak hazırlanmıştır. " +
                              $"Kategoride {category.VideoCount} video trend listesinde yer almaktadır.",
                Category = category.Name,
                Script = $"""
                    1. Giriş: {category.Name} kategorisindeki trend konulara değinin.
                    2. Popüler etiketler: {string.Join(", ", topTags)} konularını ele alın.
                    3. {(topVideo is not null ? $"En çok izlenen video: {topVideo.Title} - benzer bir format kullanın." : "Dikkat çekici bir giriş yapın.")}
                    4. İzleyici etkileşimi için soru sorun.
                    5. Kapanış ve abone çağrısı.
                    """,
                Tags = topTags,
                ThumbnailIdea = $"Dikkat çekici renkler, büyük metin ve {category.Name} temalı görseller kullanın",
                TargetAudience = $"{category.Name} içerikleri izleyen Türkiye'deki izleyiciler",
                EstimatedDuration = "8-12 dakika",
                WhyItWorks = $"Bu kategori şu anda trend listesinde {category.VideoCount} video ile temsil ediliyor " +
                             $"ve ortalama {category.AverageViews:N0} izlenme almakta."
            });
        }

        return suggestions;
    }
}

file class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }
}

file class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

file class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
