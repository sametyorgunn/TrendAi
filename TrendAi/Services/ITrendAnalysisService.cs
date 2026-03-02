using TrendAi.Models;

namespace TrendAi.Services;

public interface ITrendAnalysisService
{
    TrendAnalysisResult AnalyzeTrends(List<TrendingVideo> videos, string region);
}
