using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrendVideoAi.Models;
using TrendVideoAi.Services;

namespace TrendVideoAi.Pages;

public class IndexModel : PageModel
{
    private readonly IYouTubeTrendService _youtubeService;

    public IndexModel(IYouTubeTrendService youtubeService)
    {
        _youtubeService = youtubeService;
    }

    public List<TrendingVideo> Videos { get; set; } = [];
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
            Videos = await _youtubeService.GetTrendingVideosAsync(RegionCode);

            if (Videos.Count == 0)
                ErrorMessage = "Trend video bulunamadı. API anahtarınızı kontrol edin.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Videolar yüklenirken hata oluştu: {ex.Message}";
        }

        return Page();
    }
}
