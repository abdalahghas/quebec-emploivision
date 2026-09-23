namespace QuebecEmploiVision.Models;

/// <summary>Erreur de validation détectée sur une ligne pendant l'import.</summary>
public class ImportRowError
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = "";
    public string Expected { get; set; } = "";
    public string Received { get; set; } = "";
    public string Severity { get; set; } = ""; // ERROR / WARNING
    public string Message { get; set; } = "";
}

/// <summary>Ligne brute extraite d'un CSV, avant toute transformation.</summary>
public class RawRow
{
    public int RowNumber { get; set; }
    public string[] Fields { get; set; } = Array.Empty<string>();
    public List<ImportRowError> Errors { get; } = new();
}

/// <summary>Une observation transformée, prête pour l'entrepôt.</summary>
public class StagedObservation
{
    public int RowNumber { get; set; }
    public string RegionCode { get; set; } = "";
    public string ProfessionCode { get; set; } = "";
    public string IndustryCode { get; set; } = "";
    public int Year { get; set; }
    public int? Month { get; set; }
    public int? EmploymentCount { get; set; }
    public int? VacancyCount { get; set; }
    public decimal? AverageWage { get; set; }
    public decimal? UnemploymentRate { get; set; }
}

/// <summary>Journal d'une étape du pipeline (Extract, Validate, Transform, Load).</summary>
public class PipelineStepLog
{
    public string Step { get; set; } = "";
    public bool Success { get; set; }
    public double DurationSeconds { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>Résultat complet d'un import ETL.</summary>
public class ImportResult
{
    public int ImportRunId { get; set; }
    public string DatasetName { get; set; } = "";
    public string SourceFileName { get; set; } = "";
    public string Status { get; set; } = "";
    public int RowsProcessed { get; set; }
    public int RowsAccepted { get; set; }
    public int RowsRejected { get; set; }
    public int RowsWarning { get; set; }
    public double TotalDurationSeconds { get; set; }
    public List<PipelineStepLog> Steps { get; } = new();
    public List<ImportRowError> Errors { get; } = new();
}

/// <summary>Types de jeux de données reconnus par le pipeline.</summary>
public enum DatasetType
{
    EmploymentByRegionIndustry,
    VacanciesByRegion,
    WagesByProfession
}
