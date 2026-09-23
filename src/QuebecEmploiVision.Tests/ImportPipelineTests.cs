using QuebecEmploiVision.Models;
using QuebecEmploiVision.Services;
using Xunit;

namespace QuebecEmploiVision.Tests;

/// <summary>Tests du pipeline ETL hors base : extraction, entête, transformation.</summary>
public class ImportPipelineTests
{
    [Fact]
    public void Extraction_Ignore_Entete_Et_Lignes_Vides()
    {
        var csv = new CsvIngestionService();
        var rows = csv.Extract("RegionCode,IndustryCode,Year,Month,EmploymentCount\n06,54,2026,,1000\n\n03,54,2026,,2000\n");
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);      // numéro de ligne d'origine conservé
        Assert.Equal(4, rows[1].RowNumber);
    }

    [Fact]
    public void Entete_Conforme_Est_Reconnue()
    {
        var csv = new CsvIngestionService();
        var header = new[] { "RegionCode", "IndustryCode", "Year", "Month", "EmploymentCount" };
        Assert.True(csv.HeaderMatches(DatasetType.EmploymentByRegionIndustry, header));
        Assert.False(csv.HeaderMatches(DatasetType.VacanciesByRegion, header));
    }

    [Fact]
    public void Entete_Mauvais_Format_Est_Refusee()
    {
        var csv = new CsvIngestionService();
        var header = new[] { "region", "emploi", "annee" };
        Assert.False(csv.HeaderMatches(DatasetType.EmploymentByRegionIndustry, header));
    }

    [Fact]
    public void Transform_Emplois_Mappe_Les_Colonnes()
    {
        var csv = new CsvIngestionService();
        var transform = new TransformService(csv);
        var row = new RawRow { RowNumber = 2, Fields = new[] { "06", "54", "2026", "3", "41200" } };
        var observation = transform.Transform(row, DatasetType.EmploymentByRegionIndustry);

        Assert.Equal("06", observation.RegionCode);
        Assert.Equal("54", observation.IndustryCode);
        Assert.Equal(2026, observation.Year);
        Assert.Equal(3, observation.Month);
        Assert.Equal(41200, observation.EmploymentCount);
        Assert.Empty(row.Errors);
    }

    [Fact]
    public void Transform_Salaires_Mappe_Les_Colonnes()
    {
        var csv = new CsvIngestionService();
        var transform = new TransformService(csv);
        var row = new RawRow { RowNumber = 2, Fields = new[] { "06", "21222", "2026", "", "48.75" } };
        var observation = transform.Transform(row, DatasetType.WagesByProfession);

        Assert.Equal("21222", observation.ProfessionCode);
        Assert.Equal(48.75m, observation.AverageWage);
    }

    [Fact]
    public void Transform_Colonnes_Manquantes_Signale_Erreur()
    {
        var csv = new CsvIngestionService();
        var transform = new TransformService(csv);
        var row = new RawRow { RowNumber = 2, Fields = new[] { "06", "54" } };
        var observation = transform.Transform(row, DatasetType.EmploymentByRegionIndustry);
        Assert.NotEmpty(row.Errors);
        Assert.Contains(row.Errors, e => e.ColumnName == "Year");
    }

    [Fact]
    public void Pipeline_Complet_Sur_Fichier_Valide()
    {
        var csv = new CsvIngestionService();
        var transform = new TransformService(csv);
        var content = "RegionCode,IndustryCode,Year,Month,EmploymentCount\n06,54,2026,,88000\n03,54,2026,,42000\n";
        var rows = csv.Extract(content);

        var validator = new ValidationService(new[] { "06", "03" },
            new[] { "21222" }, new[] { "54" });
        var batch = rows.Select(r => (transform.Transform(r, DatasetType.EmploymentByRegionIndustry), r)).ToList();
        validator.DetectDuplicates(batch);
        foreach (var (observation, source) in batch) validator.Validate(observation, source);

        Assert.All(batch, b => Assert.Empty(b.r.Errors));
    }

    [Fact]
    public void Pipeline_Complet_Sur_Fichier_Avec_Erreurs()
    {
        var csv = new CsvIngestionService();
        var transform = new TransformService(csv);
        var content = "RegionCode,IndustryCode,Year,Month,EmploymentCount\n" +
                       "06,54,2026,,91000\n" +     // valide
                       "99,54,2026,,1000\n" +       // région inconnue
                       "03,54,2026,ABC,2000\n" +    // mois non numérique
                       "16,62,1899,,-500\n" +       // année impossible + négatif
                       "06,54,2026,,91000\n";       // doublon
        var rows = csv.Extract(content);

        var validator = new ValidationService(new[] { "06", "03", "16" },
            new[] { "21222" }, new[] { "54", "62" });
        var batch = rows.Select(r => (transform.Transform(r, DatasetType.EmploymentByRegionIndustry), r)).ToList();
        validator.DetectDuplicates(batch);
        foreach (var (observation, source) in batch) validator.Validate(observation, source);

        Assert.Equal(5, batch.Count);
        Assert.Empty(batch[0].r.Errors);                          // ligne valide
        Assert.Contains(batch[1].r.Errors, e => e.ColumnName == "RegionCode");
        Assert.Contains(batch[2].r.Errors, e => e.ColumnName == "Month");
        Assert.Contains(batch[3].r.Errors, e => e.ColumnName == "Year");
        Assert.Contains(batch[3].r.Errors, e => e.ColumnName == "EmploymentCount");
        Assert.Contains(batch[4].r.Errors, e => e.Message.Contains("Doublon"));
    }
}
