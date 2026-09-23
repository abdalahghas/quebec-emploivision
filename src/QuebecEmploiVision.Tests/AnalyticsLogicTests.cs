using QuebecEmploiVision.DTOs;
using QuebecEmploiVision.Services;
using Xunit;

namespace QuebecEmploiVision.Tests;

public class ForecastServiceTests
{
    private static List<TimeSeriesPoint> Series(params decimal[] values) =>
        values.Select((v, i) => new TimeSeriesPoint { Label = (2020 + i).ToString(), Value = v }).ToList();

    [Fact]
    public void Tendance_Lineaire_Suivie_Exactement()
    {
        var forecast = new ForecastService().Forecast(Series(100, 110, 120, 130, 140), 3);
        Assert.Equal(3, forecast.Forecast.Count);
        Assert.Equal(150, forecast.Forecast[0].Value);
        Assert.Equal(160, forecast.Forecast[1].Value);
        Assert.Equal(170, forecast.Forecast[2].Value);
        Assert.Equal(1.0m, forecast.RSquared);       // ajustement parfait
        Assert.Equal(10m, forecast.SlopePerPeriod);
    }

    [Fact]
    public void R2_Faible_Sur_Donnees_Bruitees()
    {
        var forecast = new ForecastService().Forecast(Series(100, 90, 120, 95, 105, 85), 2);
        Assert.True(forecast.RSquared < 0.5m);
    }

    [Fact]
    public void Trop_Peu_D_Observations_Retourne_Vide()
    {
        var forecast = new ForecastService().Forecast(Series(10, 20), 5);
        Assert.Empty(forecast.Forecast);
    }

    [Fact]
    public void Label_Annuel_Et_Mensuel_Avancent_Correctement()
    {
        var annual = new ForecastService().Forecast(Series(100, 110, 120, 130), 2);
        Assert.Equal("2024", annual.Forecast[0].Label);
        Assert.Equal("2025", annual.Forecast[1].Label);

        var monthly = new ForecastService().Forecast(new List<TimeSeriesPoint>
        {
            new() { Label = "2026-09", Value = 100 },
            new() { Label = "2026-10", Value = 102 },
            new() { Label = "2026-11", Value = 104 }
        }, 3);
        Assert.Equal("2026-12", monthly.Forecast[0].Label);
        Assert.Equal("2027-01", monthly.Forecast[1].Label);
        Assert.Equal("2027-02", monthly.Forecast[2].Label);
    }

    [Fact]
    public void La_Methodologie_Est_Explicite()
    {
        var forecast = new ForecastService().Forecast(Series(1, 2, 3, 4), 1);
        Assert.Contains("tendance", forecast.Methodology, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("extrapolation", forecast.Methodology, StringComparison.OrdinalIgnoreCase);
    }
}

public class AnomalyServiceTests
{
    private static AnomalyService.AnomalyCandidate C(string region, string period, decimal variation) =>
        new() { Region = region, Indicator = "Emploi", Period = period, VariationPct = variation };

    [Fact]
    public void Variation_Extreme_Est_Signalée()
    {
        var candidates = new List<AnomalyService.AnomalyCandidate>
        {
            C("Montréal", "2020", 1.5m), C("Montréal", "2021", 2.0m), C("Montréal", "2022", 3.0m),
            C("Montréal", "2023", 2.5m), C("Montréal", "2024", 2.2m),
            C("Montréal", "2025", 31.8m)      // bien au-delà de l'historique
        };
        var anomalies = new AnomalyService().Detect(candidates);
        var anomaly = Assert.Single(anomalies);
        Assert.Equal("Montréal", anomaly.Region);
        Assert.Equal(31.8m, anomaly.ObservedVariationPct);
        Assert.Equal("Review recommended", anomaly.Status);
    }

    [Fact]
    public void Variation_Normale_N_Est_Pas_Signalée()
    {
        var candidates = new List<AnomalyService.AnomalyCandidate>
        {
            C("Laval", "2020", 1.5m), C("Laval", "2021", 2.0m), C("Laval", "2022", 1.8m),
            C("Laval", "2023", 2.2m), C("Laval", "2024", 2.0m), C("Laval", "2025", 1.9m)
        };
        Assert.Empty(new AnomalyService().Detect(candidates));
    }

    [Fact]
    public void Historique_Trop_Court_Est_Ignore()
    {
        var candidates = new List<AnomalyService.AnomalyCandidate>
        {
            C("Gaspésie", "2024", 5m), C("Gaspésie", "2025", 400m)
        };
        Assert.Empty(new AnomalyService().Detect(candidates));
    }
}

public class DataQualityServiceTests
{
    [Fact]
    public void Score_Parfait_Sur_Import_Sans_Rejet()
    {
        var score = new DataQualityService().Score(new QualityInputs
        {
            RowsTotal = 1000, RowsTypeValid = 1000,
            RequiredCellsTotal = 4000, RequiredCellsPresent = 4000,
            ObservationsTotal = 1000, DuplicateRows = 0,
            ForeignKeysTotal = 1000, ForeignKeysResolved = 1000
        });
        Assert.Equal(100m, score.Overall);
    }

    [Fact]
    public void Rejets_Degrade_Le_Score_Proprement()
    {
        var score = new DataQualityService().Score(new QualityInputs
        {
            RowsTotal = 100, RowsTypeValid = 97,
            RequiredCellsTotal = 400, RequiredCellsPresent = 398,
            ObservationsTotal = 100, DuplicateRows = 3,
            ForeignKeysTotal = 100, ForeignKeysResolved = 97
        });
        Assert.True(score.Overall > 90 && score.Overall < 100);
        Assert.True(score.Uniqueness < 100);
    }

    [Fact]
    public void Entree_Vide_Donne_Zero_Sans_Division_Par_Zero()
    {
        var score = new DataQualityService().Score(new QualityInputs());
        Assert.Equal(0m, score.Overall);
    }

    [Fact]
    public void Le_Score_Est_Explicable_Composante_Par_Composante()
    {
        var score = new DataQualityService().Score(new QualityInputs
        {
            RowsTotal = 10, RowsTypeValid = 8,
            RequiredCellsTotal = 20, RequiredCellsPresent = 18,
            ObservationsTotal = 10, DuplicateRows = 0,
            ForeignKeysTotal = 10, ForeignKeysResolved = 10
        });
        Assert.Equal(80m, score.Validity);
        Assert.Equal(90m, score.Completeness);
        Assert.Equal(100m, score.Uniqueness);
        Assert.Equal(100m, score.Consistency);
        Assert.Equal(92.5m, score.Overall);
    }
}
