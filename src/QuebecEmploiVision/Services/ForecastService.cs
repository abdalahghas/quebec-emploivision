using System.Globalization;
using QuebecEmploiVision.DTOs;

namespace QuebecEmploiVision.Services;

/// <summary>
/// Prévision par tendance linéaire (régression des moindres carrés sur l'index
/// temporel). L'objectif est méthodologique : montrer comment un modèle
/// statistique simple et explicable s'intègre dans une application Web.
/// Ce modèle ne prédit pas l'économie : il extrapole la tendance passée.
/// </summary>
public class ForecastService
{
    public ForecastResponse Forecast(List<TimeSeriesPoint> history, int horizon)
    {
        var response = new ForecastResponse
        {
            Model = "Linear Trend (moindres carrés)",
            TrainingObservations = history.Count,
            ForecastHorizon = horizon,
            LastTrained = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            Methodology =
                "Régression linéaire sur l'index temporel (x = 0..n-1). " +
                "Les périodes prédites sont une extrapolation de la tendance historique : " +
                "le modèle ignore les chocs économiques et les effets saisonniers. " +
                "À utiliser comme indication de tendance, jamais comme promesse."
        };

        if (history.Count < 3)
        {
            return response; // pas assez d'observations pour une régression honnête
        }

        int n = history.Count;
        decimal sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
        for (int i = 0; i < n; i++)
        {
            decimal x = i, y = history[i].Value;
            sumX += x; sumY += y; sumXY += x * y; sumXX += x * x;
        }
        decimal denominator = n * sumXX - sumX * sumX;
        decimal slope = denominator == 0 ? 0 : (n * sumXY - sumX * sumY) / denominator;
        decimal intercept = (sumY - slope * sumX) / n;

        // R² = corrélation au carré entre x et y.
        decimal meanY = sumY / n;
        decimal ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            decimal fitted = intercept + slope * i;
            decimal residual = history[i].Value - fitted;
            ssTot += (history[i].Value - meanY) * (history[i].Value - meanY);
            ssRes += residual * residual;
        }
        decimal r2 = ssTot == 0 ? 0 : 1 - ssRes / ssTot;
        if (r2 < 0) r2 = 0;

        response.SlopePerPeriod = Math.Round(slope, 2);
        response.RSquared = Math.Round(r2, 4);
        response.History = history;

        var last = history[^1];
        var forecast = new List<TimeSeriesPoint>();
        for (int h = 1; h <= horizon; h++)
        {
            forecast.Add(new TimeSeriesPoint
            {
                Label = NextLabel(last.Label, h),
                Value = Math.Round(intercept + slope * (n - 1 + h), 0)
            });
        }
        response.Forecast = forecast;
        return response;
    }

    /// <summary>Pousse un label de période de h positions : "2024" → "2025", "2024-06" → "2024-07".</summary>
    private static string NextLabel(string label, int h)
    {
        if (label.Contains('-'))
        {
            var parts = label.Split('-');
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]) + h;
            year += (month - 1) / 12;
            month = (month - 1) % 12 + 1;
            return $"{year}-{month:00}";
        }
        int simpleYear = int.Parse(label) + h;
        return simpleYear.ToString();
    }
}
