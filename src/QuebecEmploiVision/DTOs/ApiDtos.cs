using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.DTOs;

/// <summary>KPI du portrait du marché, générés depuis SQL Server.</summary>
public class KpiSet
{
    public decimal? Employment { get; set; }
    public decimal? Vacancies { get; set; }
    public decimal? AverageWage { get; set; }
    public decimal? EmploymentVariationPct { get; set; }
    public string Period { get; set; } = "";
}

/// <summary>Un point d'une série temporelle.</summary>
public class TimeSeriesPoint
{
    public string Label { get; set; } = "";     // ex: "2024" ou "2024-06"
    public decimal Value { get; set; }
}

/// <summary>Réponse du dashboard : KPI + séries + liste des régions.</summary>
public class DashboardResponse
{
    public KpiSet Kpi { get; set; } = new();
    public List<TimeSeriesPoint> EmploymentHistory { get; set; } = new();
    public List<TimeSeriesPoint> VacancyHistory { get; set; } = new();
    public List<TimeSeriesPoint> WageHistory { get; set; } = new();
    public List<string> AvailableYears { get; set; } = new();
}

/// <summary>Ligne d'une fiche profession/industrie avec ses indicateurs.</summary>
public class IndicatorCard
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal? Employment { get; set; }
    public decimal? AverageWage { get; set; }
    public decimal? Vacancies { get; set; }
    public decimal? VariationPct { get; set; }
    public List<TimeSeriesPoint> History { get; set; } = new();
}

/// <summary>Comparaison côte à côte de deux régions.</summary>
public class ComparisonResponse
{
    public string LeftRegion { get; set; } = "";
    public string RightRegion { get; set; } = "";
    public List<ComparisonMetric> Metrics { get; set; } = new();
    public List<TimeSeriesPoint> LeftHistory { get; set; } = new();
    public List<TimeSeriesPoint> RightHistory { get; set; } = new();
}

public class ComparisonMetric
{
    public string Metric { get; set; } = "";
    public decimal? LeftValue { get; set; }
    public decimal? RightValue { get; set; }
}

/// <summary>Réponse du module de prévision.</summary>
public class ForecastResponse
{
    public string Model { get; set; } = "Linear Trend";
    public int TrainingObservations { get; set; }
    public int ForecastHorizon { get; set; }
    public decimal RSquared { get; set; }
    public decimal SlopePerPeriod { get; set; }
    public string LastTrained { get; set; } = "";
    public List<TimeSeriesPoint> History { get; set; } = new();
    public List<TimeSeriesPoint> Forecast { get; set; } = new();
    public string Methodology { get; set; } = "";
}

/// <summary>Score de qualité des données avec ses quatre composantes.</summary>
public class QualityScore
{
    public decimal Overall { get; set; }
    public decimal Completeness { get; set; }
    public decimal Validity { get; set; }
    public decimal Uniqueness { get; set; }
    public decimal Consistency { get; set; }
}

/// <summary>Anomalie statistique détectée (IQR), à vérifier par un humain.</summary>
public class AnomalyReport
{
    public string Region { get; set; } = "";
    public string Indicator { get; set; } = "";
    public decimal ObservedVariationPct { get; set; }
    public decimal HistoricalLowPct { get; set; }
    public decimal HistoricalHighPct { get; set; }
    public string Status { get; set; } = "Review recommended";
}

/// <summary>Statut du pipeline pour la page de surveillance.</summary>
public class PipelineStatus
{
    public string Health { get; set; } = "UNKNOWN";
    public List<PipelineSource> Sources { get; set; } = new();
    public ImportResult? LastPipeline { get; set; }
}

public class PipelineSource
{
    public string Source { get; set; } = "";
    public string LastImport { get; set; } = "";
    public bool Ok { get; set; }
}

public class ImportSummary
{
    public int ImportRunId { get; set; }
    public string DatasetName { get; set; } = "";
    public string SourceFileName { get; set; } = "";
    public string StartedAt { get; set; } = "";
    public string Status { get; set; } = "";
    public int RowsProcessed { get; set; }
    public int RowsAccepted { get; set; }
    public int RowsRejected { get; set; }
}

public class ImportDetail : ImportSummary
{
    public string FinishedAt { get; set; } = "";
    public int RowsWarning { get; set; }
    public List<ImportError> Errors { get; } = new();
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = "";
    public string Expected { get; set; } = "";
    public string Received { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}
