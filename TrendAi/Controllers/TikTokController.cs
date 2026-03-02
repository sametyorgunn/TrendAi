using Microsoft.AspNetCore.Mvc;
using TrendAi.Services;
using TrendAi.ViewModels;

namespace TrendAi.Controllers;

public class TikTokController : Controller
{
    private readonly ITikTokTrendService _tikTokService;
    private readonly ITikTokAnalysisService _analysisService;
    private readonly IAiVideoGeneratorService _aiService;
    private readonly ITikTokDownloaderService _downloaderService;
    private readonly ITikTokPublishService _publishService;

    public TikTokController(
        ITikTokTrendService tikTokService,
        ITikTokAnalysisService analysisService,
        IAiVideoGeneratorService aiService,
        ITikTokDownloaderService downloaderService,
        ITikTokPublishService publishService)
    {
        _tikTokService = tikTokService;
        _analysisService = analysisService;
        _aiService = aiService;
        _downloaderService = downloaderService;
        _publishService = publishService;
    }

    public IActionResult Index()
    {
        return View(new TikTokTrendViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string regionCode)
    {
        var vm = new TikTokTrendViewModel { RegionCode = regionCode, IsLoaded = true };

        try
        {
            vm.Videos = await _tikTokService.GetTrendingVideosAsync(regionCode);
            if (vm.Videos.Count == 0)
                vm.ErrorMessage = "TikTok trend videosu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"TikTok videoları yüklenirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Analysis()
    {
        return View(new TikTokAnalysisViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Analysis(string regionCode)
    {
        var vm = new TikTokAnalysisViewModel { RegionCode = regionCode, IsLoaded = true };

        try
        {
            var videos = await _tikTokService.GetTrendingVideosAsync(regionCode);
            if (videos.Count == 0)
            {
                vm.ErrorMessage = "TikTok trend videosu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.Analyze(videos, regionCode);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Analiz sırasında hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Generate(string? region)
    {
        var vm = new TikTokGenerateViewModel();
        if (!string.IsNullOrEmpty(region))
            vm.RegionCode = region;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(string regionCode, int ideaCount)
    {
        var vm = new TikTokGenerateViewModel { RegionCode = regionCode, IdeaCount = ideaCount, IsLoaded = true };

        try
        {
            var videos = await _tikTokService.GetTrendingVideosAsync(regionCode);
            if (videos.Count == 0)
            {
                vm.ErrorMessage = "TikTok trend videosu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.Analyze(videos, regionCode);
            vm.Suggestions = await _aiService.GenerateTikTokIdeasAsync(vm.Analysis, ideaCount);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Video fikirleri üretilirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    // ========== İNDİR & PAYLAŞ ==========

    public async Task<IActionResult> Download()
    {
        var vm = new TikTokDownloadViewModel
        {
            IsAuthenticated = _publishService.IsAuthenticated,
            AuthUrl = _publishService.GetAuthorizationUrl()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Download(string regionCode, int downloadCount)
    {
        var vm = new TikTokDownloadViewModel
        {
            RegionCode = regionCode,
            DownloadCount = downloadCount,
            IsLoaded = true,
            IsAuthenticated = _publishService.IsAuthenticated,
            AuthUrl = _publishService.GetAuthorizationUrl()
        };

        try
        {
            var videos = await _tikTokService.GetTrendingVideosAsync(regionCode);
            if (videos.Count == 0)
            {
                vm.ErrorMessage = "TikTok trend videosu bulunamadı.";
                return View(vm);
            }

            vm.Videos = videos;
            vm.DownloadResults = await _downloaderService.DownloadMultipleAsync(videos, downloadCount);
            vm.IsDownloaded = true;

            var successCount = vm.DownloadResults.Count(r => r.Success);
            vm.SuccessMessage = $"{successCount}/{downloadCount} video başarıyla indirildi. Klasör: {_downloaderService.GetDownloadsFolder()}";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"İndirme sırasında hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> PublishVideo(string videoPath, string description, string hashtags)
    {
        var tagList = string.IsNullOrEmpty(hashtags)
            ? null
            : hashtags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var result = await _publishService.PublishVideoAsync(videoPath, description, tagList);

        TempData["PublishResult"] = result.Success
            ? $"✅ Video başarıyla paylaşıldı! Publish ID: {result.PublishId}"
            : $"❌ Paylaşım hatası: {result.ErrorMessage}";

        return RedirectToAction(nameof(Download));
    }

    // TikTok OAuth Callback
    public async Task<IActionResult> Callback(string code, string state)
    {
        if (string.IsNullOrEmpty(code))
        {
            TempData["PublishResult"] = "❌ TikTok yetkilendirme başarısız.";
            return RedirectToAction(nameof(Download));
        }

        var success = await _publishService.ExchangeCodeForTokenAsync(code);

        TempData["PublishResult"] = success
            ? "✅ TikTok hesabı başarıyla bağlandı!"
            : "❌ Token alınamadı.";

        return RedirectToAction(nameof(Download));
    }
}
