using TrendVideoAi.Models;
using TrendVideoAi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<YouTubeApiSettings>(builder.Configuration.GetSection("YouTubeApi"));
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));

builder.Services.AddSingleton<IYouTubeTrendService, YouTubeTrendService>();
builder.Services.AddSingleton<ITrendAnalysisService, TrendAnalysisService>();
builder.Services.AddHttpClient<IAiVideoGeneratorService, AiVideoGeneratorService>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
