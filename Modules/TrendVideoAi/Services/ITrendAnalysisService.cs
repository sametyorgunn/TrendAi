using TrendVideoAi.Models;

namespace TrendVideoAi.Services;

public interface ITrendAnalysisService
{
    TrendAnalysisResult AnalyzeTrends(List<TrendingVideo> videos, string region);
}
