using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrendVideoAi.Models;
using TrendVideoAi.Services;

namespace TrendVideoAi.Pages;

public class AnalysisModel : PageModel
{
    private readonly IYouTubeTrendService _youtubeService;
    private readonly ITrendAnalysisService _analysisService;

    public AnalysisModel(IYouTubeTrendService youtubeService, ITrendAnalysisService analysisService)
    {
        _youtubeService = youtubeService;
        _analysisService = analysisService;
    }

    public TrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string RegionCode { get; set; } = "TR";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsLoaded = true;

        try
        {
            var videos = await _youtubeService.GetTrendingVideosAsync(RegionCode);

            if (videos.Count == 0)
            {
                ErrorMessage = "Trend video bulunamadı. API anahtarınızı kontrol edin.";
                return Page();
            }

            Analysis = _analysisService.AnalyzeTrends(videos, RegionCode);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analiz sırasında hata oluştu: {ex.Message}";
        }

        return Page();
    }
}
