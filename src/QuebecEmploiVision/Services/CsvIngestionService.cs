using System.Globalization;
using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.Services;

/// <summary>
/// EXTRACT : lecture d'un CSV et validation structurelle (colonnes, types).
/// La ligne 1 est l'entête. Toute ligne malformée est signalée, jamais ignorée
/// silencieusement.
/// </summary>
public class CsvIngestionService
{
    private static readonly Dictionary<DatasetType, string[]> ExpectedHeaders = new()
    {
        [DatasetType.EmploymentByRegionIndustry] =
            ["RegionCode", "IndustryCode", "Year", "Month", "EmploymentCount"],
        [DatasetType.VacanciesByRegion] =
            ["RegionCode", "Year", "Month", "VacancyCount"],
        [DatasetType.WagesByProfession] =
            ["RegionCode", "ProfessionCode", "Year", "Month", "AverageWage"],
    };

    public string[] ExpectedHeader(DatasetType type) => ExpectedHeaders[type];

    /// <summary>Valide l'entête du fichier par rapport au format attendu du type.</summary>
    public bool HeaderMatches(DatasetType type, string[] header) =>
        header.Length == ExpectedHeaders[type].Length
        && header.Select(h => h.Trim()).SequenceEqual(ExpectedHeaders[type], StringComparer.OrdinalIgnoreCase);

    /// <summary>Lit un champ en signalant une erreur propre si la colonne est absente.</summary>
    private static string? Field(RawRow row, int index, string columnName)
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
            return null;
        }
        return row.Fields[index].Trim();
    }

    /// <summary>Lit toutes les lignes du texte CSV en gardant le numéro de ligne d'origine.</summary>
    public List<RawRow> Extract(string csvContent)
    {
        var rows = new List<RawRow>();
        var lines = csvContent.Replace("\r\n", "\n").Split('\n');
        int lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line)) continue; // entête / vide
            var fields = line.Split(',');
            rows.Add(new RawRow { RowNumber = lineNumber, Fields = fields });
        }
        return rows;
    }

    /// <summary>Tente de convertir un champ en entier (nullable) ; null si vide, erreur si non numérique.</summary>
    public int? ParseInt(RawRow row, int index, string columnName)
    {
        var value = Field(row, index, columnName);
        if (value is null) return null;   // colonne absente : erreur déjà journalisée
        if (value.Length == 0) return null;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            row.Errors.Add(new ImportRowError
            {
                RowNumber = row.RowNumber,
                ColumnName = columnName,
                Expected = "Integer",
                Received = value,
                Severity = "ERROR",
                Message = $"Valeur non numérique dans la colonne {columnName}."
            });
            return null;
        }
        return result;
    }

    /// <summary>Tente de convertir un champ en décimal (nullable) ; null si vide, erreur si non numérique.</summary>
    public decimal? ParseDecimal(RawRow row, int index, string columnName)
    {
        var value = Field(row, index, columnName);
        if (value is null) return null;   // colonne absente : erreur déjà journalisée
        if (value.Length == 0) return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            row.Errors.Add(new ImportRowError
            {
                RowNumber = row.RowNumber,
                ColumnName = columnName,
                Expected = "Decimal",
                Received = value,
                Severity = "ERROR",
                Message = $"Valeur non numérique dans la colonne {columnName}."
            });
            return null;
        }
        return result;
    }
}
