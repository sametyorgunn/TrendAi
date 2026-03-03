using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TrendAi.Models;

namespace TrendAi.Services;

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
                messages = new object[]
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

            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseString);
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
        return $$"""
            Aşağıdaki YouTube trend verilerine dayanarak {{count}} adet viral olma potansiyeli yüksek video fikri üret.

            {{trendSummary}}

            Her video fikri için şu bilgileri JSON dizisi formatında ver:
            [
              {
                "title": "Video başlığı (dikkat çekici, tıklanabilir)",
                "description": "Video açıklaması (SEO uyumlu, 2-3 cümle)",
                "category": "Hangi kategoride olacağı",
                "script": "Videonun kısa senaryosu/akış planı (5-10 cümle)",
                "tags": ["etiket1", "etiket2", "etiket3", "etiket4", "etiket5"],
                "thumbnailIdea": "Thumbnail (küçük resim) tasarım önerisi",
                "targetAudience": "Hedef kitle açıklaması",
                "estimatedDuration": "Tahmini video süresi",
                "whyItWorks": "Bu videonun neden tutacağının kısa açıklaması"
              }
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
                Script = $"1. Giriş: {category.Name} kategorisindeki trend konulara değinin.\n" +
                         $"2. Popüler etiketler: {string.Join(", ", topTags)} konularını ele alın.\n" +
                         (topVideo is not null
                             ? $"3. En çok izlenen video: {topVideo.Title} - benzer bir format kullanın.\n"
                             : "3. Dikkat çekici bir giriş yapın.\n") +
                         "4. İzleyici etkileşimi için soru sorun.\n" +
                         "5. Kapanış ve abone çağrısı.",
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

    public async Task<List<AiVideoSuggestion>> GenerateTikTokIdeasAsync(TikTokTrendAnalysisResult analysis, int count = 5)
    {
        try
        {
            var summary = BuildTikTokSummary(analysis);
            var prompt = BuildTikTokPrompt(summary, count);

            var requestBody = new
            {
                model = _settings.Model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                            Sen bir TikTok içerik stratejisti ve viral video uzmanısın.
                            Trend analizlerine dayanarak FYP (For You Page) üzerinde viral olma potansiyeli yüksek kısa video fikirleri üretiyorsun.
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
                _logger.LogError("OpenAI API hatası (TikTok): {Status} - {Response}", response.StatusCode, responseString);
                return GenerateTikTokFallback(analysis, count);
            }

            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseString);
            var messageContent = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(messageContent))
                return GenerateTikTokFallback(analysis, count);

            var jsonStart = messageContent.IndexOf('[');
            var jsonEnd = messageContent.LastIndexOf(']');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                messageContent = messageContent[jsonStart..(jsonEnd + 1)];

            var suggestions = JsonSerializer.Deserialize<List<AiVideoSuggestion>>(messageContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return suggestions ?? GenerateTikTokFallback(analysis, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok AI önerileri oluşturulurken hata oluştu");
            return GenerateTikTokFallback(analysis, count);
        }
    }

    private static string BuildTikTokSummary(TikTokTrendAnalysisResult analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Bölge: {analysis.Region}");
        sb.AppendLine($"Analiz edilen video sayısı: {analysis.TotalVideosAnalyzed}");
        sb.AppendLine($"Toplam izlenme: {analysis.TotalPlays:N0}");
        sb.AppendLine($"Ortalama etkileşim oranı: %{analysis.AvgEngagementRate:F2}");
        sb.AppendLine();

        sb.AppendLine("=== TREND HASHTAG'LER ===");
        foreach (var tag in analysis.TopHashtags.Take(10))
        {
            sb.AppendLine($"  #{tag.Tag} - {tag.VideoCount} video, {tag.FormattedPlays} izlenme, etkileşim: %{tag.EngagementRate:F2}");
        }

        sb.AppendLine();
        sb.AppendLine("=== TREND MÜZİKLER ===");
        foreach (var music in analysis.TopMusic.Take(10))
        {
            sb.AppendLine($"  🎵 {music.Title} - {music.VideoCount} video");
        }

        sb.AppendLine();
        sb.AppendLine("=== EN ÇOK İZLENEN VİDEOLAR ===");
        foreach (var video in analysis.AllVideos.OrderByDescending(v => v.PlayCount).Take(5))
        {
            sb.AppendLine($"  \"{video.Description[..Math.Min(80, video.Description.Length)]}\" - {video.FormattedPlays} izlenme, @{video.AuthorName}");
        }

        return sb.ToString();
    }

    private static string BuildTikTokPrompt(string summary, int count)
    {
        return $$"""
            Aşağıdaki TikTok trend verilerine dayanarak {{count}} adet FYP'de viral olma potansiyeli yüksek TikTok video fikri üret.

            {{summary}}

            Her video fikri için şu bilgileri JSON dizisi formatında ver:
            [
              {
                "title": "Video konsepti (kısa, dikkat çekici)",
                "description": "Video açıklaması ve hook cümlesi",
                "category": "İçerik kategorisi",
                "script": "Videonun saniye saniye akış planı (15-60 saniyelik TikTok formatında)",
                "tags": ["hashtag1", "hashtag2", "hashtag3", "hashtag4", "hashtag5"],
                "thumbnailIdea": "Kapak karesi / ilk frame önerisi",
                "targetAudience": "Hedef kitle",
                "estimatedDuration": "Tahmini süre (15s/30s/60s)",
                "whyItWorks": "Neden viral olacağının açıklaması"
              }
            ]

            SADECE JSON dizisi döndür, başka hiçbir metin ekleme.
            """;
    }

    private static List<AiVideoSuggestion> GenerateTikTokFallback(TikTokTrendAnalysisResult analysis, int count)
    {
        var suggestions = new List<AiVideoSuggestion>();

        foreach (var hashtag in analysis.TopHashtags.Take(count))
        {
            suggestions.Add(new AiVideoSuggestion
            {
                Title = $"#{hashtag.Tag} Trendinde Viral TikTok Fikri",
                Description = $"#{hashtag.Tag} hashtag'i şu anda trendde! {hashtag.VideoCount} video bu hashtag ile paylaşılmış.",
                Category = "TikTok Trend",
                Script = $"0-3s: Dikkat çekici hook - \"Bunu bilmiyordunuz!\"\n" +
                         $"3-15s: #{hashtag.Tag} konusunda ana içerik\n" +
                         "15-25s: Sürpriz veya plot twist\n" +
                         "25-30s: CTA - \"Takip et, daha fazlası için!\"",
                Tags = [$"{hashtag.Tag}", .. analysis.TopHashtags.Take(4).Select(h => h.Tag)],
                ThumbnailIdea = "İlk karede dikkat çekici ifade veya metin overlay",
                TargetAudience = "TikTok'ta trend içerik takip eden Z kuşağı ve genç yetişkinler",
                EstimatedDuration = "30 saniye",
                WhyItWorks = $"#{hashtag.Tag} trendinde {hashtag.VideoCount} video var ve toplam {hashtag.FormattedPlays} izlenme almış."
            });
        }

        return suggestions;
    }

    public async Task<List<AiVideoSuggestion>> GenerateInstagramIdeasAsync(InstagramTrendAnalysisResult analysis, int count = 5)
    {
        try
        {
            var summary = BuildInstagramSummary(analysis);
            var prompt = BuildInstagramPrompt(summary, count);

            var requestBody = new
            {
                model = _settings.Model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                            Sen bir Instagram Reels içerik stratejisti ve viral video uzmanısın.
                            Trend analizlerine dayanarak Keşfet sayfasında öne çıkma potansiyeli yüksek kısa video fikirleri üretiyorsun.
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
                _logger.LogError("OpenAI API hatası (Instagram): {Status} - {Response}", response.StatusCode, responseString);
                return GenerateInstagramFallback(analysis, count);
            }

            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseString);
            var messageContent = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(messageContent))
                return GenerateInstagramFallback(analysis, count);

            var jsonStart = messageContent.IndexOf('[');
            var jsonEnd = messageContent.LastIndexOf(']');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                messageContent = messageContent[jsonStart..(jsonEnd + 1)];

            var suggestions = JsonSerializer.Deserialize<List<AiVideoSuggestion>>(messageContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return suggestions ?? GenerateInstagramFallback(analysis, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Instagram AI önerileri oluşturulurken hata oluştu");
            return GenerateInstagramFallback(analysis, count);
        }
    }

    private static string BuildInstagramSummary(InstagramTrendAnalysisResult analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Kategori/Hashtag: #{analysis.Category}");
        sb.AppendLine($"Analiz edilen post sayısı: {analysis.TotalPostsAnalyzed}");
        sb.AppendLine($"Toplam izlenme: {analysis.TotalViews:N0}");
        sb.AppendLine($"Toplam beğeni: {analysis.TotalLikes:N0}");
        sb.AppendLine($"Ortalama etkileşim oranı: %{analysis.AvgEngagementRate:F2}");
        sb.AppendLine();

        sb.AppendLine("=== TREND HASHTAG'LER ===");
        foreach (var tag in analysis.TopHashtags.Take(15))
        {
            sb.AppendLine($"  #{tag.Tag} - {tag.PostCount} post, {tag.FormattedViews} izlenme, etkileşim: %{tag.EngagementRate:F2}");
        }

        if (analysis.TopMusic.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== TREND MÜZİKLER ===");
            foreach (var music in analysis.TopMusic.Take(10))
            {
                sb.AppendLine($"  🎵 {music.Title} - {music.PostCount} post");
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== EN ÇOK İZLENEN REELS ===");
        foreach (var post in analysis.AllPosts.OrderByDescending(p => p.ViewCount > 0 ? p.ViewCount : p.LikeCount).Take(5))
        {
            var preview = post.Caption.Length > 80 ? post.Caption[..80] + "..." : post.Caption;
            sb.AppendLine($"  \"{preview}\" - {post.FormattedViews} izlenme, @{post.AuthorUsername}");
        }

        return sb.ToString();
    }

    private static string BuildInstagramPrompt(string summary, int count)
    {
        return $$"""
            Aşağıdaki Instagram Reels trend verilerine dayanarak {{count}} adet Keşfet sayfasında öne çıkma potansiyeli yüksek Instagram Reels fikri üret.

            {{summary}}

            Her Reels fikri için şu bilgileri JSON dizisi formatında ver:
            [
              {
                "title": "Reel konsepti (kısa, dikkat çekici)",
                "description": "Caption ve hook cümlesi (Instagram formatında, hashtag önerileri dahil)",
                "category": "İçerik kategorisi",
                "script": "Reels'ın saniye saniye akış planı (15-90 saniyelik format, hook+içerik+CTA yapısı)",
                "tags": ["hashtag1", "hashtag2", "hashtag3", "hashtag4", "hashtag5"],
                "thumbnailIdea": "Kapak karesi / ilk frame önerisi (dikkat çekici, metin overlay önerisi)",
                "targetAudience": "Hedef kitle",
                "estimatedDuration": "Tahmini süre (15s/30s/60s/90s)",
                "whyItWorks": "Neden Keşfet'e düşeceğinin açıklaması"
              }
            ]

            SADECE JSON dizisi döndür, başka hiçbir metin ekleme.
            """;
    }

    private static List<AiVideoSuggestion> GenerateInstagramFallback(InstagramTrendAnalysisResult analysis, int count)
    {
        var suggestions = new List<AiVideoSuggestion>();

        foreach (var hashtag in analysis.TopHashtags.Take(count))
        {
            suggestions.Add(new AiVideoSuggestion
            {
                Title = $"#{hashtag.Tag} Trendinde Viral Reels Fikri",
                Description = $"#{hashtag.Tag} hashtag'i şu anda trendde! {hashtag.PostCount} post bu hashtag ile paylaşılmış. Keşfet'e düşme ihtimali yüksek.",
                Category = "Instagram Reels",
                Script = $"0-3s: Hook - Dikkat çekici açılış karesi\n" +
                         $"3-20s: #{hashtag.Tag} konusunda ana içerik\n" +
                         "20-25s: Değer katma / sürpriz an\n" +
                         "25-30s: CTA - \"Beğen ve takip et!\"",
                Tags = [$"{hashtag.Tag}", .. analysis.TopHashtags.Skip(1).Take(4).Select(h => h.Tag)],
                ThumbnailIdea = "Parlak renkli arka plan, büyük metin overlay ve emoji kullanımı",
                TargetAudience = "Instagram'da Keşfet sayfasını aktif kullanan kullanıcılar",
                EstimatedDuration = "30 saniye",
                WhyItWorks = $"#{hashtag.Tag} trendinde {hashtag.PostCount} post var ve toplam {hashtag.FormattedViews} izlenme almış."
            });
        }

        return suggestions;
    }
}

public class OpenAiChatResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }
}

public class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
