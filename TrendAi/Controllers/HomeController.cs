using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TrendAi.Models;
using TrendAi.Services;
using TrendAi.ViewModels;

namespace TrendAi.Controllers;

public class HomeController : Controller
{
    private readonly IYouTubeTrendService _youtubeService;
    private readonly ITrendAnalysisService _analysisService;
    private readonly IAiVideoGeneratorService _aiService;

    public HomeController(
        IYouTubeTrendService youtubeService,
        ITrendAnalysisService analysisService,
        IAiVideoGeneratorService aiService)
    {
        _youtubeService = youtubeService;
        _analysisService = analysisService;
        _aiService = aiService;
    }

    public IActionResult Index()
    {
        return View(new TrendIndexViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string regionCode)
    {
        var vm = new TrendIndexViewModel { RegionCode = regionCode, IsLoaded = true };

        try
        {
            vm.Videos = await _youtubeService.GetTrendingVideosAsync(regionCode);
            if (vm.Videos.Count == 0)
                vm.ErrorMessage = "Trend video bulunamadı. API anahtarınızı kontrol edin.";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Videolar yüklenirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Analysis()
    {
        return View(new AnalysisViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Analysis(string regionCode)
    {
        var vm = new AnalysisViewModel { RegionCode = regionCode, IsLoaded = true };

        try
        {
            var videos = await _youtubeService.GetTrendingVideosAsync(regionCode);
            if (videos.Count == 0)
            {
                vm.ErrorMessage = "Trend video bulunamadı. API anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.AnalyzeTrends(videos, regionCode);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Analiz sırasında hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Generate(string? region)
    {
        var vm = new GenerateViewModel();
        if (!string.IsNullOrEmpty(region))
            vm.RegionCode = region;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(string regionCode, int ideaCount)
    {
        var vm = new GenerateViewModel { RegionCode = regionCode, IdeaCount = ideaCount, IsLoaded = true };

        try
        {
            var videos = await _youtubeService.GetTrendingVideosAsync(regionCode);
            if (videos.Count == 0)
            {
                vm.ErrorMessage = "Trend video bulunamadı. API anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.AnalyzeTrends(videos, regionCode);
            vm.Suggestions = await _aiService.GenerateVideoIdeasAsync(vm.Analysis, ideaCount);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Video fikirleri üretilirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
