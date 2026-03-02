using TrendAi.Models;

namespace TrendAi.Services;

public interface ITikTokAnalysisService
{
    TikTokTrendAnalysisResult Analyze(List<TikTokVideo> videos, string region);
}
