using Microsoft.AspNetCore.Mvc;
using QuebecEmploiVision.DTOs;
using QuebecEmploiVision.Models;
using QuebecEmploiVision.Services;

namespace QuebecEmploiVision.Controllers;

/// <summary>Tous les contrôleurs API, volontairement fins : ils valident les
/// entrées, délèguent aux services et retournent des erreurs lisibles.</summary>

[ApiController]
[Route("api")]
public class EmploiVisionController : ControllerBase
{
    private readonly AnalyticsService _analytics;
    private readonly ForecastService _forecast;
    private readonly AnomalyService _anomalies;
    private readonly ImportService _import;

    public EmploiVisionController(AnalyticsService analytics, ForecastService forecast,
                                 AnomalyService anomalies, ImportService import)
    {
        _analytics = analytics;
        _forecast = forecast;
        _anomalies = anomalies;
        _import = import;
    }

    private IActionResult Guard(Func<Task<IActionResult>> action) =>
        Wrap(action);

    private static IActionResult Wrap(Func<Task<IActionResult>> action) =>
        action().GetAwaiter().GetResult();

    // GET /api/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(string? region, string? industry, int? year)
    {
        try { return Ok(await _analytics.DashboardAsync(region, industry, year)); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/regions
    [HttpGet("regions")]
    public async Task<IActionResult> Regions()
    {
        try { return Ok(await _analytics.RegionsAsync()); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/regions/{name}
    [HttpGet("regions/{name}")]
    public async Task<IActionResult> Region(string name, int? year)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Nom de région requis.");
        try { return Ok(await _analytics.RegionAsync(name, year)); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/industries
    [HttpGet("industries")]
    public async Task<IActionResult> Industries(int? year)
    {
        try { return Ok(await _analytics.IndustriesAsync(year)); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/professions?search=developpeur
    [HttpGet("professions")]
    public async Task<IActionResult> Professions(string? search, int? year)
    {
        try { return Ok(await _analytics.ProfessionsAsync(search, year)); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/comparison?left=Montréal&right=Capitale-Nationale
    [HttpGet("comparison")]
    public async Task<IActionResult> Comparison(string left, string right, int? year)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return BadRequest("Deux régions sont requises pour la comparaison.");
        try { return Ok(await _analytics.CompareAsync(left, right, year)); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/forecast?region=Montréal&horizon=6
    [HttpGet("forecast")]
    public async Task<IActionResult> Forecast(string region, string industry, int horizon = 5)
    {
        if (horizon is < 1 or > 24)
            return BadRequest("L'horizon doit être entre 1 et 24 périodes.");
        try
        {
            var dashboard = await _analytics.DashboardAsync(region, string.IsNullOrEmpty(industry) ? null : industry, null);
            if (dashboard.EmploymentHistory.Count < 3)
                return NotFound("Pas assez d'observations historiques pour entraîner le modèle.");
            return Ok(_forecast.Forecast(dashboard.EmploymentHistory, horizon));
        }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/data-quality
    [HttpGet("data-quality")]
    public async Task<IActionResult> DataQuality()
    {
        try
        {
            var status = await _analytics.PipelineAsync();
            var last = status.LastPipeline;
            var score = new DataQualityService().Score(new QualityInputs
            {
                RowsTotal = last?.RowsProcessed ?? 0,
                RowsTypeValid = last?.RowsAccepted ?? 0,
                RequiredCellsPresent = last?.RowsAccepted ?? 0,
                RequiredCellsTotal = last?.RowsProcessed ?? 0,
                ObservationsTotal = last?.RowsProcessed ?? 0,
                DuplicateRows = last?.RowsRejected ?? 0,
                ForeignKeysResolved = last?.RowsAccepted ?? 0,
                ForeignKeysTotal = last?.RowsProcessed ?? 0
            });
            var candidates = await _analytics.AnomalyCandidatesAsync();
            return Ok(new { score, anomalies = _anomalies.Detect(candidates) });
        }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/imports
    [HttpGet("imports")]
    public async Task<IActionResult> Imports()
    {
        try { return Ok(await _analytics.ImportHistoryAsync()); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/imports/{id}
    [HttpGet("imports/{id}")]
    public async Task<IActionResult> Import(int id)
    {
        try
        {
            var detail = await _analytics.ImportDetailAsync(id);
            return detail is null ? NotFound("Import introuvable.") : Ok(detail);
        }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // GET /api/pipeline
    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline()
    {
        try { return Ok(await _analytics.PipelineAsync()); }
        catch (Exception) { return ServiceUnavailable(); }
    }

    // POST /api/import  (formulaire en multipart : dataset + fichier CSV)
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] string dataset, IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(dataset))
            return BadRequest("Type de dataset requis.");
        if (file is null || file.Length == 0)
            return BadRequest("Fichier CSV requis.");
        if (file.Length > 20_000_000)
            return BadRequest("Fichier trop volumineux (max 20 Mo).");
        if (!Enum.TryParse<DatasetType>(dataset, ignoreCase: true, out var type))
            return BadRequest($"Type de dataset inconnu : {dataset}.");

        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync();
        try
        {
            var result = await _import.ImportAsync(type, file.FileName, csvContent);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem($"Import interrompu : {ex.Message}");
        }
    }

    private ObjectResult ServiceUnavailable() =>
        StatusCode(503, new { message = "Impossible de récupérer les données. Réessayez plus tard." });
}
