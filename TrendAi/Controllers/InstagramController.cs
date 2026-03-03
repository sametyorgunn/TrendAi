using Microsoft.AspNetCore.Mvc;
using TrendAi.Services;
using TrendAi.ViewModels;

namespace TrendAi.Controllers;

public class InstagramController : Controller
{
    private readonly IInstagramTrendService _instagramService;
    private readonly IInstagramAnalysisService _analysisService;
    private readonly IAiVideoGeneratorService _aiService;

    public InstagramController(
        IInstagramTrendService instagramService,
        IInstagramAnalysisService analysisService,
        IAiVideoGeneratorService aiService)
    {
        _instagramService = instagramService;
        _analysisService = analysisService;
        _aiService = aiService;
    }

    public IActionResult Index()
    {
        return View(new InstagramTrendViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(string hashtag)
    {
        var vm = new InstagramTrendViewModel { Hashtag = hashtag, IsLoaded = true };

        try
        {
            vm.Posts = await _instagramService.GetTrendingPostsAsync(hashtag);
            if (vm.Posts.Count == 0)
                vm.ErrorMessage = "Instagram trend postu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Instagram postları yüklenirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Analysis()
    {
        return View(new InstagramAnalysisViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Analysis(string hashtag)
    {
        var vm = new InstagramAnalysisViewModel { Hashtag = hashtag, IsLoaded = true };

        try
        {
            var posts = await _instagramService.GetTrendingPostsAsync(hashtag);
            if (posts.Count == 0)
            {
                vm.ErrorMessage = "Instagram trend postu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.Analyze(posts, hashtag);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Analiz sırasında hata oluştu: {ex.Message}";
        }

        return View(vm);
    }

    public IActionResult Generate(string? hashtag)
    {
        var vm = new InstagramGenerateViewModel();
        if (!string.IsNullOrEmpty(hashtag))
            vm.Hashtag = hashtag;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Generate(string hashtag, int ideaCount)
    {
        var vm = new InstagramGenerateViewModel { Hashtag = hashtag, IdeaCount = ideaCount, IsLoaded = true };

        try
        {
            var posts = await _instagramService.GetTrendingPostsAsync(hashtag);
            if (posts.Count == 0)
            {
                vm.ErrorMessage = "Instagram trend postu bulunamadı. RapidAPI anahtarınızı kontrol edin.";
                return View(vm);
            }

            vm.Analysis = _analysisService.Analyze(posts, hashtag);
            vm.Suggestions = await _aiService.GenerateInstagramIdeasAsync(vm.Analysis, ideaCount);
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Video fikirleri üretilirken hata oluştu: {ex.Message}";
        }

        return View(vm);
    }
}
