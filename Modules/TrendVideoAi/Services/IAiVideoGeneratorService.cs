using TrendVideoAi.Models;

namespace TrendVideoAi.Services;

public interface IAiVideoGeneratorService
{
    Task<List<AiVideoSuggestion>> GenerateVideoIdeasAsync(TrendAnalysisResult analysis, int count = 5);
}
