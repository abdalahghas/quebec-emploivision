using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.Services;

/// <summary>
/// TRANSFORM : traduit une ligne brute en observation prête pour l'entrepôt.
/// Une seule responsabilité : le mapping colonnes → champs, sans jugement
/// (la validation reste dans ValidationService).
/// </summary>
public class TransformService
{
    private readonly CsvIngestionService _csv;

    public TransformService(CsvIngestionService csv) => _csv = csv;

    public StagedObservation Transform(RawRow row, DatasetType type) => type switch
    {
        DatasetType.EmploymentByRegionIndustry => TransformEmployment(row),
        DatasetType.VacanciesByRegion => TransformVacancies(row),
        DatasetType.WagesByProfession => TransformWages(row),
        _ => throw new NotSupportedException($"Type de dataset inconnu : {type}")
    };

    private StagedObservation Base(RawRow row, int regionIndex)
    {
        return new StagedObservation
        {
            RowNumber = row.RowNumber,
            RegionCode = Field(row, regionIndex, "RegionCode"),
        };
    }

    private StagedObservation TransformEmployment(RawRow row)
    {
        var o = Base(row, 0);
        o.IndustryCode = Field(row, 1, "IndustryCode");
        o.Year = _csv.ParseInt(row, 2, "Year") ?? 0;
        o.Month = _csv.ParseInt(row, 3, "Month");
        o.EmploymentCount = _csv.ParseInt(row, 4, "EmploymentCount");
        return o;
    }

    private StagedObservation TransformVacancies(RawRow row)
    {
        var o = Base(row, 0);
        o.Year = _csv.ParseInt(row, 1, "Year") ?? 0;
        o.Month = _csv.ParseInt(row, 2, "Month");
        o.VacancyCount = _csv.ParseInt(row, 3, "VacancyCount");
        return o;
    }

    private StagedObservation TransformWages(RawRow row)
    {
        var o = Base(row, 0);
        o.ProfessionCode = Field(row, 1, "ProfessionCode");
        o.Year = _csv.ParseInt(row, 2, "Year") ?? 0;
        o.Month = _csv.ParseInt(row, 3, "Month");
        o.AverageWage = _csv.ParseDecimal(row, 4, "AverageWage");
        return o;
    }

    private string Field(RawRow row, int index, string columnName)
    {
        if (index >= row.Fields.Length)
        {
            row.Errors.Add(new ImportRowError
            {
                RowNumber = row.RowNumber,
                ColumnName = columnName,
                Expected = "Colonne présente",
                Received = "",
                Severity = "ERROR",
                Message = $"Colonne {columnName} absente de la ligne."
            });
            return "";
        }
        return row.Fields[index].Trim();
    }
}
