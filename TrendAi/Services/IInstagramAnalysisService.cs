using TrendAi.Models;

namespace TrendAi.Services;

public interface IInstagramAnalysisService
{
    InstagramTrendAnalysisResult Analyze(List<InstagramPost> posts, string category);
}
