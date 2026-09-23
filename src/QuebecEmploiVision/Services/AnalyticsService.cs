using System.Data;
using Microsoft.Data.SqlClient;
using QuebecEmploiVision.Data;
using QuebecEmploiVision.DTOs;
using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.Services;

/// <summary>
/// Toutes les lectures analytiques de l'application. Les requêtes SQL vivent ici
/// (jointures sur le schéma en étoile) : chaque méthode correspond à une page
/// du dashboard et reste explicable en entrevue.
/// </summary>
public class AnalyticsService
{
    private readonly IDb _db;

    public AnalyticsService(IDb db) => _db = db;

    private static SqlParameter Param(string name, object? value) =>
        new(name, value is null ? DBNull.Value : value);

    private static List<TimeSeriesPoint> ToSeries(DataTable table, string labelColumn, string valueColumn) =>
        table.Rows.Cast<DataRow>()
             .Select(r => new TimeSeriesPoint
             {
                 Label = r[labelColumn].ToString()!,
                 Value = Convert.ToDecimal(r[valueColumn] == DBNull.Value ? 0 : r[valueColumn])
             })
             .ToList();

    private static decimal? NullableDecimal(DataTable table, string column)
    {
        if (table.Rows.Count == 0 || table.Rows[0][column] == DBNull.Value) return null;
        return Convert.ToDecimal(table.Rows[0][column]);
    }

    /// <summary>Régions administratives du Québec (dimension).</summary>
    public async Task<List<string>> RegionsAsync()
    {
        var table = await _db.QueryAsync("SELECT RegionName FROM DimRegion ORDER BY RegionName");
        return table.Rows.Cast<DataRow>().Select(r => r["RegionName"].ToString()!).ToList();
    }

    /// <summary>Industries disponibles.</summary>
    public async Task<List<string>> IndustriesAsync()
    {
        var table = await _db.QueryAsync("SELECT IndustryName FROM DimIndustry ORDER BY IndustryName");
        return table.Rows.Cast<DataRow>().Select(r => r["IndustryName"].ToString()!).ToList();
    }

    /// <summary>Années disponibles dans la table de faits.</summary>
    public async Task<List<string>> YearsAsync()
    {
        var table = await _db.QueryAsync(
            "SELECT DISTINCT Year FROM DimDate WHERE DateId IN (SELECT DateId FROM FactLabourMarket) ORDER BY Year");
        return table.Rows.Cast<DataRow>().Select(r => r["Year"].ToString()!).ToList();
    }

    /// <summary>
    /// Portrait du marché : KPI + séries historiques. Équivalent de la requête
    /// SELECT r.RegionName, d.Year, SUM(f.EmploymentCount) ... GROUP BY, ORDER BY.
    /// </summary>
    public async Task<DashboardResponse> DashboardAsync(string? region, string? industry, int? year)
    {
        var response = new DashboardResponse();
        response.AvailableYears = await YearsAsync();
        int selectedYear = year ?? (response.AvailableYears.Count > 0
            ? int.Parse(response.AvailableYears[^1]) : DateTime.Now.Year);
        response.Kpi.Period = selectedYear.ToString();

        response.Kpi.Employment = NullableDecimal(await _db.QueryAsync(
            """
            SELECT SUM(f.EmploymentCount) AS Value
            FROM vw_FactDetails f
            WHERE f.Year = @year
              AND (@region IS NULL OR f.RegionName = @region)
              AND (@industry IS NULL OR f.IndustryName = @industry)
            """,
            Param("@year", selectedYear), Param("@region", region), Param("@industry", industry)), "Value");

        response.Kpi.Vacancies = NullableDecimal(await _db.QueryAsync(
            """
            SELECT SUM(f.VacancyCount) AS Value
            FROM vw_FactDetails f
            WHERE f.Year = @year
              AND (@region IS NULL OR f.RegionName = @region)
              AND (@industry IS NULL OR f.IndustryName = @industry)
            """,
            Param("@year", selectedYear), Param("@region", region), Param("@industry", industry)), "Value");

        response.Kpi.AverageWage = NullableDecimal(await _db.QueryAsync(
            """
            SELECT AVG(f.AverageWage) AS Value
            FROM vw_FactDetails f
            WHERE f.Year = @year
              AND (@region IS NULL OR f.RegionName = @region)
              AND (@industry IS NULL OR f.IndustryName = @industry)
            """,
            Param("@year", selectedYear), Param("@region", region), Param("@industry", industry)), "Value");

        response.Kpi.EmploymentVariationPct = NullableDecimal(await _db.QueryAsync(
            """
            WITH Annuel AS (
                SELECT d.Year, SUM(f.EmploymentCount) AS Total
                FROM FactLabourMarket f
                JOIN DimDate d ON d.DateId = f.DateId
                JOIN DimRegion r ON r.RegionId = f.RegionId
                LEFT JOIN DimIndustry i ON i.IndustryId = f.IndustryId
                WHERE (@region IS NULL OR r.RegionName = @region)
                  AND (@industry IS NULL OR i.IndustryName = @industry)
                GROUP BY d.Year
            )
            SELECT 100.0 * (MAX(CASE WHEN Year = @year THEN Total END)
                          - MAX(CASE WHEN Year = @year - 1 THEN Total END))
                          / NULLIF(MAX(CASE WHEN Year = @year - 1 THEN Total END), 0) AS Value
            FROM Annuel
            """,
            Param("@year", selectedYear), Param("@region", region), Param("@industry", industry)), "Value");

        response.EmploymentHistory = ToSeries(await _db.QueryAsync(
            """
            SELECT CAST(d.Year AS varchar(4)) AS Label, SUM(f.EmploymentCount) AS Value
            FROM FactLabourMarket f
            JOIN DimDate d ON d.DateId = f.DateId
            JOIN DimRegion r ON r.RegionId = f.RegionId
            LEFT JOIN DimIndustry i ON i.IndustryId = f.IndustryId
            WHERE (@region IS NULL OR r.RegionName = @region)
              AND (@industry IS NULL OR i.IndustryName = @industry)
            GROUP BY d.Year
            ORDER BY d.Year
            """,
            Param("@region", region), Param("@industry", industry)), "Label", "Value");

        response.VacancyHistory = ToSeries(await _db.QueryAsync(
            """
            SELECT CAST(d.Year AS varchar(4)) AS Label, SUM(f.VacancyCount) AS Value
            FROM FactLabourMarket f
            JOIN DimDate d ON d.DateId = f.DateId
            JOIN DimRegion r ON r.RegionId = f.RegionId
            WHERE @region IS NULL OR r.RegionName = @region
            GROUP BY d.Year
            ORDER BY d.Year
            """,
            Param("@region", region)), "Label", "Value");

        response.WageHistory = ToSeries(await _db.QueryAsync(
            """
            SELECT CAST(d.Year AS varchar(4)) AS Label, AVG(f.AverageWage) AS Value
            FROM FactLabourMarket f
            JOIN DimDate d ON d.DateId = f.DateId
            JOIN DimRegion r ON r.RegionId = f.RegionId
            WHERE @region IS NULL OR r.RegionName = @region
            GROUP BY d.Year
            ORDER BY d.Year
            """,
            Param("@region", region)), "Label", "Value");

        return response;
    }

    /// <summary>Fiches professions, filtrables par recherche et année.</summary>
    public async Task<List<IndicatorCard>> ProfessionsAsync(string? search, int? year)
    {
        var pattern = search is null ? null : $"%{search}%";
        var table = await _db.QueryAsync(
            """
            SELECT p.ProfessionCode AS Code, p.ProfessionName AS Name,
                   SUM(f.EmploymentCount) AS Employment,
                   AVG(f.AverageWage) AS AverageWage,
                   SUM(f.VacancyCount) AS Vacancies
            FROM DimProfession p
            LEFT JOIN FactLabourMarket f ON f.ProfessionId = p.ProfessionId
            LEFT JOIN DimDate d ON d.DateId = f.DateId
            WHERE (@pattern IS NULL OR p.ProfessionName LIKE @pattern)
              AND (@year IS NULL OR d.Year = @year)
            GROUP BY p.ProfessionCode, p.ProfessionName
            ORDER BY SUM(f.EmploymentCount) DESC
            """,
            Param("@pattern", pattern), Param("@year", year));

        return table.Rows.Cast<DataRow>().Select(r => new IndicatorCard
        {
            Code = r["Code"].ToString()!,
            Name = r["Name"].ToString()!,
            Employment = r["Employment"] == DBNull.Value ? null : Convert.ToDecimal(r["Employment"]),
            AverageWage = r["AverageWage"] == DBNull.Value ? null : Math.Round(Convert.ToDecimal(r["AverageWage"]), 2),
            Vacancies = r["Vacancies"] == DBNull.Value ? null : Convert.ToDecimal(r["Vacancies"]),
            VariationPct = null
        }).ToList();
    }

    /// <summary>Fiches industries.</summary>
    public async Task<List<IndicatorCard>> IndustriesAsync(int? year)
    {
        var table = await _db.QueryAsync(
            """
            SELECT i.IndustryCode AS Code, i.IndustryName AS Name,
                   SUM(f.EmploymentCount) AS Employment,
                   AVG(f.AverageWage) AS AverageWage,
                   SUM(f.VacancyCount) AS Vacancies
            FROM DimIndustry i
            LEFT JOIN FactLabourMarket f ON f.IndustryId = i.IndustryId
            LEFT JOIN DimDate d ON d.DateId = f.DateId
            WHERE @year IS NULL OR d.Year = @year
            GROUP BY i.IndustryCode, i.IndustryName
            ORDER BY SUM(f.EmploymentCount) DESC
            """,
            Param("@year", year));

        return table.Rows.Cast<DataRow>().Select(r => new IndicatorCard
        {
            Code = r["Code"].ToString()!,
            Name = r["Name"].ToString()!,
            Employment = r["Employment"] == DBNull.Value ? null : Convert.ToDecimal(r["Employment"]),
            AverageWage = r["AverageWage"] == DBNull.Value ? null : Math.Round(Convert.ToDecimal(r["AverageWage"]), 2),
            Vacancies = r["Vacancies"] == DBNull.Value ? null : Convert.ToDecimal(r["Vacancies"]),
            VariationPct = null
        }).ToList();
    }

    /// <summary>Fiche d'une région : indicateurs + historique.</summary>
    public async Task<IndicatorCard> RegionAsync(string regionName, int? year)
    {
        var selectedYear = year ?? DateTime.Now.Year;
        var table = await _db.QueryAsync(
            """
            SELECT r.RegionCode AS Code, r.RegionName AS Name,
                   SUM(CASE WHEN d.Year = @year THEN f.EmploymentCount END) AS Employment,
                   AVG(CASE WHEN d.Year = @year THEN f.AverageWage END) AS AverageWage,
                   SUM(CASE WHEN d.Year = @year THEN f.VacancyCount END) AS Vacancies
            FROM DimRegion r
            JOIN FactLabourMarket f ON f.RegionId = r.RegionId
            JOIN DimDate d ON d.DateId = f.DateId
            WHERE r.RegionName = @region
            GROUP BY r.RegionCode, r.RegionName
            """,
            Param("@year", selectedYear), Param("@region", regionName));

        var card = new IndicatorCard
        {
            Code = table.Rows.Count > 0 ? table.Rows[0]["Code"].ToString()! : "",
            Name = regionName,
            Employment = NullableDecimal(table, "Employment"),
            AverageWage = NullableDecimal(table, "AverageWage") is { } w ? Math.Round(w, 2) : null,
            Vacancies = NullableDecimal(table, "Vacancies")
        };

        card.History = ToSeries(await _db.QueryAsync(
            """
            SELECT CAST(d.Year AS varchar(4)) AS Label, SUM(f.EmploymentCount) AS Value
            FROM FactLabourMarket f
            JOIN DimDate d ON d.DateId = f.DateId
            JOIN DimRegion r ON r.RegionId = f.RegionId
            WHERE r.RegionName = @region
            GROUP BY d.Year
            ORDER BY d.Year
            """,
            Param("@region", regionName)), "Label", "Value");
        return card;
    }

    /// <summary>Comparateur : deux régions côte à côte + historiques.</summary>
    public async Task<ComparisonResponse> CompareAsync(string left, string right, int? year)
    {
        var selectedYear = year ?? DateTime.Now.Year;
        var response = new ComparisonResponse { LeftRegion = left, RightRegion = right };
        var table = await _db.QueryAsync(
            """
            SELECT r.RegionName, d.Year,
                   SUM(f.EmploymentCount) AS Employment,
                   AVG(f.AverageWage) AS AverageWage,
                   SUM(f.VacancyCount) AS Vacancies
            FROM FactLabourMarket f
            JOIN DimRegion r ON r.RegionId = f.RegionId
            JOIN DimDate d ON d.DateId = f.DateId
            WHERE r.RegionName IN (@left, @right)
            GROUP BY r.RegionName, d.Year
            """,
            Param("@left", left), Param("@right", right));

        foreach (var (label, column) in new[] { ("Emploi", "Employment"), ("Salaire moyen", "AverageWage"), ("Postes vacants", "Vacancies") })
        {
            var leftValue = table.Rows.Cast<DataRow>()
                .Where(r => r["RegionName"].ToString() == left && Convert.ToInt32(r["Year"]) == selectedYear)
                .Select(r => r[column] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r[column]))
                .FirstOrDefault();
            var rightValue = table.Rows.Cast<DataRow>()
                .Where(r => r["RegionName"].ToString() == right && Convert.ToInt32(r["Year"]) == selectedYear)
                .Select(r => r[column] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r[column]))
                .FirstOrDefault();
            response.Metrics.Add(new ComparisonMetric { Metric = label, LeftValue = leftValue, RightValue = rightValue });
        }

        foreach (var (region, target) in new[] { (left, response.LeftHistory), (right, response.RightHistory) })
        {
            var series = await _db.QueryAsync(
                """
                SELECT CAST(d.Year AS varchar(4)) AS Label, SUM(f.EmploymentCount) AS Value
                FROM FactLabourMarket f
                JOIN DimDate d ON d.DateId = f.DateId
                JOIN DimRegion r ON r.RegionId = f.RegionId
                WHERE r.RegionName = @region
                GROUP BY d.Year
                ORDER BY d.Year
                """,
                Param("@region", region));
            target.AddRange(ToSeries(series, "Label", "Value"));
        }
        return response;
    }

    /// <summary>Candidats à la détection d'anomalies : variations annuelles par région.</summary>
    public async Task<List<AnomalyService.AnomalyCandidate>> AnomalyCandidatesAsync()
    {
        var table = await _db.QueryAsync(
            """
            WITH Annuel AS (
                SELECT r.RegionName, d.Year, SUM(f.EmploymentCount) AS Total
                FROM FactLabourMarket f
                JOIN DimDate d ON d.DateId = f.DateId
                JOIN DimRegion r ON r.RegionId = f.RegionId
                GROUP BY r.RegionName, d.Year
            )
            SELECT a.RegionName, 'Emploi' AS Indicator,
                   CAST(a.Year AS varchar(4)) AS Period,
                   100.0 * (a.Total - prev.Total) / NULLIF(prev.Total, 0) AS VariationPct
            FROM Annuel a
            JOIN Annuel prev ON prev.RegionName = a.RegionName AND prev.Year = a.Year - 1
            """);
        return table.Rows.Cast<DataRow>().Select(r => new AnomalyService.AnomalyCandidate
        {
            Region = r["RegionName"].ToString()!,
            Indicator = r["Indicator"].ToString()!,
            Period = r["Period"].ToString()!,
            VariationPct = Convert.ToDecimal(r["VariationPct"] == DBNull.Value ? 0 : r["VariationPct"])
        }).ToList();
    }

    /// <summary>Historique des imports (page Imports).</summary>
    public async Task<List<ImportSummary>> ImportHistoryAsync()
    {
        var table = await _db.QueryAsync(
            """
            SELECT ImportRunId, DatasetName, SourceFileName, StartedAt, Status,
                   RowsProcessed, RowsAccepted, RowsRejected
            FROM ImportRun ORDER BY StartedAt DESC
            """);
        return table.Rows.Cast<DataRow>().Select(r => new ImportSummary
        {
            ImportRunId = Convert.ToInt32(r["ImportRunId"]),
            DatasetName = r["DatasetName"].ToString()!,
            SourceFileName = r["SourceFileName"].ToString()!,
            StartedAt = r["StartedAt"].ToString()!,
            Status = r["Status"].ToString()!,
            RowsProcessed = Convert.ToInt32(r["RowsProcessed"]),
            RowsAccepted = Convert.ToInt32(r["RowsAccepted"]),
            RowsRejected = Convert.ToInt32(r["RowsRejected"])
        }).ToList();
    }

    /// <summary>Détail d'un import : comptes, durée, erreurs ligne par ligne.</summary>
    public async Task<ImportDetail?> ImportDetailAsync(int importRunId)
    {
        var table = await _db.QueryAsync(
            "SELECT * FROM ImportRun WHERE ImportRunId = @id", Param("@id", importRunId));
        if (table.Rows.Count == 0) return null;

        var row = table.Rows[0];
        var detail = new ImportDetail
        {
            ImportRunId = importRunId,
            DatasetName = row["DatasetName"].ToString()!,
            SourceFileName = row["SourceFileName"].ToString()!,
            StartedAt = row["StartedAt"].ToString()!,
            FinishedAt = row["FinishedAt"].ToString()!,
            RowsProcessed = Convert.ToInt32(row["RowsProcessed"]),
            RowsAccepted = Convert.ToInt32(row["RowsAccepted"]),
            RowsRejected = Convert.ToInt32(row["RowsRejected"]),
            RowsWarning = Convert.ToInt32(row["RowsWarning"]),
            Status = row["Status"].ToString()!
        };

        var errors = await _db.QueryAsync(
            "SELECT RowNumber, ColumnName, Expected, Received, Severity, Message FROM ImportRowError WHERE ImportRunId = @id",
            Param("@id", importRunId));
        detail.Errors.AddRange(errors.Rows.Cast<DataRow>().Select(e => new ImportError
        {
            RowNumber = Convert.ToInt32(e["RowNumber"]),
            ColumnName = e["ColumnName"].ToString()!,
            Expected = e["Expected"].ToString()!,
            Received = e["Received"].ToString()!,
            Severity = e["Severity"].ToString()!,
            Message = e["Message"].ToString()!
        }));
        return detail;
    }

    /// <summary>Sources du pipeline + dernier import par dataset (page Pipeline).</summary>
    public async Task<PipelineStatus> PipelineAsync()
    {
        var status = new PipelineStatus();
        var table = await _db.QueryAsync(
            """
            SELECT DatasetName, MAX(StartedAt) AS LastImport,
                   CASE WHEN MAX(CASE WHEN Status = 'FAILED' THEN 1 ELSE 0 END) = 1 THEN 0 ELSE 1 END AS Ok
            FROM ImportRun GROUP BY DatasetName
            """);
        status.Sources = table.Rows.Cast<DataRow>().Select(r => new PipelineSource
        {
            Source = r["DatasetName"].ToString()!,
            LastImport = r["LastImport"].ToString()!,
            Ok = Convert.ToInt32(r["Ok"]) == 1
        }).ToList();
        status.Health = status.Sources.Count == 0 ? "UNKNOWN"
            : status.Sources.All(s => s.Ok) ? "HEALTHY" : "DEGRADED";

        var last = await _db.QueryAsync("SELECT TOP 1 * FROM ImportRun ORDER BY StartedAt DESC");
        if (last.Rows.Count > 0)
        {
            var row = last.Rows[0];
            status.LastPipeline = new ImportResult
            {
                ImportRunId = Convert.ToInt32(row["ImportRunId"]),
                DatasetName = row["DatasetName"].ToString()!,
                Status = row["Status"].ToString()!,
                RowsProcessed = Convert.ToInt32(row["RowsProcessed"]),
                RowsAccepted = Convert.ToInt32(row["RowsAccepted"]),
                RowsRejected = Convert.ToInt32(row["RowsRejected"]),
                RowsWarning = Convert.ToInt32(row["RowsWarning"])
            };
        }
        return status;
    }
}
