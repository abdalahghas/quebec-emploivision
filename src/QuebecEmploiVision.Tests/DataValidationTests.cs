using QuebecEmploiVision.Models;
using QuebecEmploiVision.Services;
using Xunit;

namespace QuebecEmploiVision.Tests;

/// <summary>Règles de validation métier, indépendantes de la base de données.</summary>
public class DataValidationTests
{
    private static ValidationService Validator(params string[] regionCodes) =>
        new(regionCodes, new[] { "21222" }, new[] { "54", "62" });

    private static RawRow Row(params string[] fields) =>
        new() { RowNumber = 2, Fields = fields };

    [Fact]
    public void Accepte_Enregistrement_Valide()
    {
        var observation = new StagedObservation
        {
            RegionCode = "06", IndustryCode = "54", Year = 2026, EmploymentCount = 1000
        };
        Assert.True(Validator("06", "03", "16").Validate(observation, Row("06", "54", "2026", "", "1000")));
    }

    [Fact]
    public void Rejete_Entier_Invalide()
    {
        var csv = new CsvIngestionService();
        var row = Row("06", "54", "2026", "", "ABC");
        Assert.Null(csv.ParseInt(row, 4, "EmploymentCount"));
        var error = Assert.Single(row.Errors);
        Assert.Equal("EmploymentCount", error.ColumnName);
        Assert.Equal("Integer", error.Expected);
        Assert.Equal("ABC", error.Received);
    }

    [Fact]
    public void Rejete_Region_Manhquante()
    {
        var observation = new StagedObservation { RegionCode = "", Year = 2026, EmploymentCount = 5 };
        Assert.False(Validator("06").Validate(observation, Row("", "", "2026", "", "5")));
    }

    [Fact]
    public void Rejete_Region_Inconnue()
    {
        var observation = new StagedObservation { RegionCode = "99", Year = 2026, EmploymentCount = 5 };
        var source = Row("99", "", "2026", "", "5");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "RegionCode");
    }

    [Fact]
    public void Rejete_Annee_Impossible()
    {
        var observation = new StagedObservation { RegionCode = "06", Year = 1899, EmploymentCount = 5 };
        var source = Row("06", "", "1899", "", "5");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "Year");
    }

    [Fact]
    public void Rejete_Effectif_Negatif()
    {
        var observation = new StagedObservation { RegionCode = "06", Year = 2026, EmploymentCount = -50 };
        var source = Row("06", "", "2026", "", "-50");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "EmploymentCount");
    }

    [Fact]
    public void Rejete_Salaire_Negatif()
    {
        var observation = new StagedObservation { RegionCode = "06", Year = 2026, AverageWage = -10m };
        var source = Row("06", "", "2026", "", "");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "AverageWage");
    }

    [Fact]
    public void Rejete_Taux_Chomage_Hors_Bornes()
    {
        var observation = new StagedObservation { RegionCode = "06", Year = 2026, UnemploymentRate = 142m };
        var source = Row("06", "", "2026", "", "");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "UnemploymentRate");
    }

    [Fact]
    public void Rejete_Mois_Hors_Bornes()
    {
        var observation = new StagedObservation { RegionCode = "06", Year = 2026, Month = 13 };
        var source = Row("06", "", "2026", "13", "");
        Assert.False(Validator("06").Validate(observation, source));
        Assert.Contains(source.Errors, e => e.ColumnName == "Month");
    }

    [Fact]
    public void Detecte_Doublon_Dans_Le_Lot()
    {
        var validator = Validator("06");
        var first = new StagedObservation { RegionCode = "06", IndustryCode = "54", Year = 2026, EmploymentCount = 1 };
        var second = new StagedObservation { RegionCode = "06", IndustryCode = "54", Year = 2026, EmploymentCount = 1 };
        var batch = new List<(StagedObservation, RawRow)>
        {
            (first, Row("06", "54", "2026", "", "1")),
            (second, Row("06", "54", "2026", "", "1"))
        };
        validator.DetectDuplicates(batch);
        Assert.Empty(batch[0].Item2.Errors);
        Assert.Contains(batch[1].Item2.Errors, e => e.Message.Contains("Doublon"));
    }

    [Fact]
    public void Cle_D_Unicite_Distingue_Les_Periodes()
    {
        var a = new StagedObservation { RegionCode = "06", IndustryCode = "54", Year = 2026, Month = 1 };
        var b = new StagedObservation { RegionCode = "06", IndustryCode = "54", Year = 2026, Month = 2 };
        Assert.NotEqual(ValidationService.ObservationKey(a), ValidationService.ObservationKey(b));
    }
}
