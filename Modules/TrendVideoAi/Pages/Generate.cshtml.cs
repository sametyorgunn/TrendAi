using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrendVideoAi.Models;
using TrendVideoAi.Services;

namespace TrendVideoAi.Pages;

public class GenerateModel : PageModel
{
    private readonly IYouTubeTrendService _youtubeService;
    private readonly ITrendAnalysisService _analysisService;
    private readonly IAiVideoGeneratorService _aiService;

    public GenerateModel(
        IYouTubeTrendService youtubeService,
        ITrendAnalysisService analysisService,
        IAiVideoGeneratorService aiService)
    {
        _youtubeService = youtubeService;
        _analysisService = analysisService;
        _aiService = aiService;
    }

    public List<AiVideoSuggestion> Suggestions { get; set; } = [];
    public TrendAnalysisResult? Analysis { get; set; }
    public bool IsLoaded { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string RegionCode { get; set; } = "TR";

    [BindProperty]
    public int IdeaCount { get; set; } = 5;

    public void OnGet(string? region)
    {
        if (!string.IsNullOrEmpty(region))
            RegionCode = region;
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
            Suggestions = await _aiService.GenerateVideoIdeasAsync(Analysis, IdeaCount);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Video fikirleri üretilirken hata oluştu: {ex.Message}";
        }

        return Page();
    }
}
