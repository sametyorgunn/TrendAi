using TrendAi.Models;

namespace TrendAi.Services;

public interface IAiVideoGeneratorService
{
    Task<List<AiVideoSuggestion>> GenerateVideoIdeasAsync(TrendAnalysisResult analysis, int count = 5);
    Task<List<AiVideoSuggestion>> GenerateTikTokIdeasAsync(TikTokTrendAnalysisResult analysis, int count = 5);
    Task<List<AiVideoSuggestion>> GenerateInstagramIdeasAsync(InstagramTrendAnalysisResult analysis, int count = 5);
}
