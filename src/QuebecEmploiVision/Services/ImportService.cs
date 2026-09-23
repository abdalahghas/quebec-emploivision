using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using QuebecEmploiVision.Data;
using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.Services;

/// <summary>
/// LOAD + orchestration du pipeline ETL :
///   1. EXTRACT   : lecture CSV + validation structurelle;
///   2. VALIDATE  : règles métier, référentiels, doublons;
///   3. TRANSFORM : mapping en observations;
///   4. LOAD      : staging SQL Server puis procédure stockée vers l'entrepôt.
/// Chaque import est historisé (ImportRun) et chaque rejet est journalisé
/// (ImportRowError) : l'administrateur voit exactement ce qui s'est passé.
/// </summary>
public class ImportService
{
    private readonly IDb _db;
    private readonly CsvIngestionService _csv;
    private readonly TransformService _transform;

    public ImportService(IDb db, CsvIngestionService csv, TransformService transform)
    {
        _db = db;
        _csv = csv;
        _transform = transform;
    }

    public async Task<ImportResult> ImportAsync(DatasetType type, string fileName, string csvContent)
    {
        var result = new ImportResult { DatasetName = type.ToString(), SourceFileName = fileName };
        var total = Stopwatch.StartNew();

        // --- EXTRACT ---
        var extractWatch = Stopwatch.StartNew();
        List<RawRow> rawRows;
        try
        {
            var header = FirstLine(csvContent);
            if (!_csv.HeaderMatches(type, header))
            {
                result.Status = "FAILED";
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = 1,
                    ColumnName = "(entête)",
                    Expected = string.Join(", ", _csv.ExpectedHeader(type)),
                    Received = string.Join(", ", header),
                    Severity = "ERROR",
                    Message = "L'entête du fichier ne correspond pas au format attendu."
                });
                result.Steps.Add(Failed("Extract", "Format de fichier invalide."));
                await SaveRunAsync(result);
                return result;
            }
            rawRows = _csv.Extract(csvContent);
        }
        catch (Exception ex)
        {
            result.Status = "FAILED";
            result.Steps.Add(Failed("Extract", ex.Message));
            await SaveRunAsync(result);
            return result;
        }
        extractWatch.Stop();
        result.RowsProcessed = rawRows.Count;
        result.Steps.Add(Step("Extract", extractWatch.Elapsed.TotalSeconds));

        // --- TRANSFORM + VALIDATE ---
        var validateWatch = Stopwatch.StartNew();
        var referentials = await LoadReferentialsAsync();
        var validator = new ValidationService(
            referentials.Regions, referentials.Professions, referentials.Industries);

        var batch = new List<(StagedObservation Observation, RawRow Source)>();
        foreach (var row in rawRows)
        {
            var observation = _transform.Transform(row, type);
            batch.Add((observation, row));
        }
        validator.DetectDuplicates(batch);

        foreach (var (observation, source) in batch)
        {
            validator.Validate(observation, source);
        }

        var accepted = batch.Where(b => b.Source.Errors.All(e => e.Severity != "ERROR")).ToList();
        var rejected = batch.Where(b => b.Source.Errors.Any(e => e.Severity == "ERROR")).ToList();
        var warnings = batch.Sum(b => b.Source.Errors.Count(e => e.Severity == "WARNING"));
        validateWatch.Stop();
        result.Steps.Add(Step("Validate", validateWatch.Elapsed.TotalSeconds));
        result.RowsAccepted = accepted.Count;
        result.RowsRejected = rejected.Count;
        result.RowsWarning = warnings;
        result.Errors.AddRange(rejected.SelectMany(b => b.Source.Errors.Where(e => e.Severity == "ERROR")));

        // --- LOAD (staging puis entrepôt) ---
        var loadWatch = Stopwatch.StartNew();
        try
        {
            await _db.ExecuteAsync("TRUNCATE TABLE StagingLabourObservation");
            await LoadStagingAsync(accepted.Select(b => b.Observation));
            await _db.ExecuteAsync("EXEC usp_LoadStagingToFact");
            await _db.ExecuteAsync("TRUNCATE TABLE StagingLabourObservation");
        }
        catch (Exception ex)
        {
            result.Status = "FAILED";
            result.Steps.Add(Failed("Load", ex.Message));
            await SaveRunAsync(result);
            return result;
        }
        loadWatch.Stop();
        result.Steps.Add(Step("Load", loadWatch.Elapsed.TotalSeconds));

        result.Status = result.RowsRejected > 0 ? "WARNING" : "SUCCESS";
        total.Stop();
        result.TotalDurationSeconds = Math.Round(total.Elapsed.TotalSeconds, 2);
        await SaveRunAsync(result);
        return result;
    }

    private static string[] FirstLine(string csvContent) =>
        (csvContent.Replace("\r\n", "\n").Split('\n').FirstOrDefault() ?? "").Split(',');

    private static PipelineStepLog Step(string name, double seconds) => new()
    {
        Step = name, Success = true, DurationSeconds = Math.Round(seconds, 2)
    };

    private static PipelineStepLog Failed(string name, string message) => new()
    {
        Step = name, Success = false, DurationSeconds = 0, Message = message
    };

    private async Task<(HashSet<string> Regions, HashSet<string> Professions, HashSet<string> Industries)> LoadReferentialsAsync()
    {
        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var professions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var industries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var regionTable = await _db.QueryAsync("SELECT RegionCode FROM DimRegion");
        foreach (DataRow row in regionTable.Rows) regions.Add(row["RegionCode"].ToString()!);
        var professionTable = await _db.QueryAsync("SELECT ProfessionCode FROM DimProfession");
        foreach (DataRow row in professionTable.Rows) professions.Add(row["ProfessionCode"].ToString()!);
        var industryTable = await _db.QueryAsync("SELECT IndustryCode FROM DimIndustry");
        foreach (DataRow row in industryTable.Rows) industries.Add(row["IndustryCode"].ToString()!);
        return (regions, professions, industries);
    }

    private async Task LoadStagingAsync(IEnumerable<StagedObservation> observations)
    {
        var table = new DataTable();
        table.Columns.Add("RegionCode", typeof(string));
        table.Columns.Add("ProfessionCode", typeof(string));
        table.Columns.Add("IndustryCode", typeof(string));
        table.Columns.Add("Year", typeof(int));
        table.Columns.Add("Month", typeof(int));
        table.Columns.Add("EmploymentCount", typeof(int));
        table.Columns.Add("VacancyCount", typeof(int));
        table.Columns.Add("AverageWage", typeof(decimal));
        table.Columns.Add("UnemploymentRate", typeof(decimal));

        foreach (var o in observations)
        {
            table.Rows.Add(o.RegionCode,
                o.ProfessionCode.Length == 0 ? (object)DBNull.Value : o.ProfessionCode,
                o.IndustryCode.Length == 0 ? (object)DBNull.Value : o.IndustryCode,
                o.Year,
                o.Month.HasValue ? o.Month.Value : (object)DBNull.Value,
                o.EmploymentCount.HasValue ? o.EmploymentCount.Value : (object)DBNull.Value,
                o.VacancyCount.HasValue ? o.VacancyCount.Value : (object)DBNull.Value,
                o.AverageWage.HasValue ? o.AverageWage.Value : (object)DBNull.Value,
                o.UnemploymentRate.HasValue ? o.UnemploymentRate.Value : (object)DBNull.Value);
        }

        using var bulk = new SqlBulkCopy(_db.ConnectionString)
        {
            DestinationTableName = "StagingLabourObservation"
        };
        bulk.ColumnMappings.Add("RegionCode", "RegionCode");
        bulk.ColumnMappings.Add("ProfessionCode", "ProfessionCode");
        bulk.ColumnMappings.Add("IndustryCode", "IndustryCode");
        bulk.ColumnMappings.Add("Year", "Year");
        bulk.ColumnMappings.Add("Month", "Month");
        bulk.ColumnMappings.Add("EmploymentCount", "EmploymentCount");
        bulk.ColumnMappings.Add("VacancyCount", "VacancyCount");
        bulk.ColumnMappings.Add("AverageWage", "AverageWage");
        bulk.ColumnMappings.Add("UnemploymentRate", "UnemploymentRate");
        await bulk.WriteToServerAsync(table);
    }

    private async Task SaveRunAsync(ImportResult result)
    {
        var sql = """
            INSERT INTO ImportRun (DatasetName, SourceFileName, StartedAt, FinishedAt,
                                   RowsProcessed, RowsAccepted, RowsRejected, RowsWarning, Status)
            OUTPUT INSERTED.ImportRunId
            VALUES (@dataset, @file, DATEADD(SECOND, -@duration, SYSUTCDATETIME()), SYSUTCDATETIME(),
                    @processed, @accepted, @rejected, @warning, @status)
            """;
        var id = await _db.ScalarAsync(sql,
            new SqlParameter("@dataset", result.DatasetName),
            new SqlParameter("@file", result.SourceFileName),
            new SqlParameter("@duration", (int)Math.Ceiling(result.TotalDurationSeconds)),
            new SqlParameter("@processed", result.RowsProcessed),
            new SqlParameter("@accepted", result.RowsAccepted),
            new SqlParameter("@rejected", result.RowsRejected),
            new SqlParameter("@warning", result.RowsWarning),
            new SqlParameter("@status", result.Status));
        result.ImportRunId = Convert.ToInt32(id);

        foreach (var error in result.Errors.Take(500))
        {
            await _db.ExecuteAsync(
                "INSERT INTO ImportRowError (ImportRunId, RowNumber, ColumnName, Expected, Received, Severity, Message) " +
                "VALUES (@run, @row, @col, @exp, @recv, @sev, @msg)",
                new SqlParameter("@run", result.ImportRunId),
                new SqlParameter("@row", error.RowNumber),
                new SqlParameter("@col", error.ColumnName),
                new SqlParameter("@exp", error.Expected),
                new SqlParameter("@recv", error.Received),
                new SqlParameter("@sev", error.Severity),
                new SqlParameter("@msg", error.Message));
        }
    }
}
