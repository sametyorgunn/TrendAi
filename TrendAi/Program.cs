using TrendAi.Models;
using TrendAi.Services;

var builder = WebApplication.CreateBuilder(args);

// YouTube & OpenAI & TikTok & Instagram ayarlarını bind et
builder.Services.Configure<YouTubeApiSettings>(builder.Configuration.GetSection("YouTubeApi"));
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));
builder.Services.Configure<TikTokApiSettings>(builder.Configuration.GetSection("TikTokApi"));
builder.Services.Configure<TikTokPublishSettings>(builder.Configuration.GetSection("TikTokPublish"));
builder.Services.Configure<InstagramApiSettings>(builder.Configuration.GetSection("InstagramApi"));

// Servisleri kaydet
builder.Services.AddSingleton<IYouTubeTrendService, YouTubeTrendService>();
builder.Services.AddSingleton<ITrendAnalysisService, TrendAnalysisService>();
builder.Services.AddSingleton<ITikTokAnalysisService, TikTokAnalysisService>();
builder.Services.AddSingleton<IInstagramAnalysisService, InstagramAnalysisService>();
builder.Services.AddHttpClient<IAiVideoGeneratorService, AiVideoGeneratorService>();
builder.Services.AddHttpClient<ITikTokTrendService, TikTokTrendService>();
builder.Services.AddHttpClient<ITikTokDownloaderService, TikTokDownloaderService>();
builder.Services.AddHttpClient<ITikTokPublishService, TikTokPublishService>();
builder.Services.AddHttpClient<IInstagramTrendService, InstagramTrendService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
