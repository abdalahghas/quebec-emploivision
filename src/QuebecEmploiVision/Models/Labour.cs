namespace QuebecEmploiVision.Models;

/// <summary>Région administrative du Québec (dimension).</summary>
public class Region
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = "";
    public string RegionName { get; set; } = "";
}

/// <summary>Profession (code CNP 2021, dimension).</summary>
public class Profession
{
    public int ProfessionId { get; set; }
    public string ProfessionCode { get; set; } = "";
    public string ProfessionName { get; set; } = "";
}

/// <summary>Industrie (code SCIAN, dimension).</summary>
public class Industry
{
    public int IndustryId { get; set; }
    public string IndustryCode { get; set; } = "";
    public string IndustryName { get; set; } = "";
}

/// <summary>Observation quantitative du marché du travail (table de faits).</summary>
public class LabourObservation
{
    public int FactId { get; set; }
    public int DateId { get; set; }
    public int RegionId { get; set; }
    public int? ProfessionId { get; set; }
    public int? IndustryId { get; set; }
    public int? EmploymentCount { get; set; }
    public int? VacancyCount { get; set; }
    public decimal? AverageWage { get; set; }
    public decimal? UnemploymentRate { get; set; }
    public int ImportRunId { get; set; }
}

/// <summary>Une exécution d'import (historique des pipelines).</summary>
public class ImportRun
{
    public int ImportRunId { get; set; }
    public string DatasetName { get; set; } = "";
    public string SourceFileName { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int RowsProcessed { get; set; }
    public int RowsAccepted { get; set; }
    public int RowsRejected { get; set; }
    public int RowsWarning { get; set; }
    public string Status { get; set; } = ""; // SUCCESS / WARNING / FAILED
}
