using QuebecEmploiVision.Data;
using QuebecEmploiVision.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IDb, SqlServerDb>();
builder.Services.AddSingleton<CsvIngestionService>();
builder.Services.AddSingleton<TransformService>();
builder.Services.AddSingleton<ImportService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSingleton<ForecastService>();
builder.Services.AddSingleton<AnomalyService>();

var app = builder.Build();

app.UseDefaultFiles();   // sert wwwroot/index.html sur /
app.UseStaticFiles();
app.MapControllers();

app.Run();
