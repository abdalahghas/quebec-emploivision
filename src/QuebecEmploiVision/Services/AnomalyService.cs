using System.Globalization;
using QuebecEmploiVision.DTOs;

namespace QuebecEmploiVision.Services;

/// <summary>
/// Détection d'anomalies par IQR (écart interquartile) sur les variations
/// historiques. L'objectif n'est pas de dire qu'une donnée est fausse :
/// c'est de signaler qu'une observation est inhabituelle et mérite vérification.
/// </summary>
public class AnomalyService
{
    private const decimal IqrMultiplier = 1.5m;

    public List<AnomalyReport> Detect(List<AnomalyCandidate> candidates)
    {
        var anomalies = new List<AnomalyReport>();
        var groups = candidates.GroupBy(c => (c.Region, c.Indicator));

        foreach (var group in groups)
        {
            var variations = group.Select(c => c.VariationPct).OrderBy(v => v).ToList();
            if (variations.Count < 5) continue; // pas assez d'historique pour juger

            decimal q1 = Quantile(variations, 0.25m);
            decimal q3 = Quantile(variations, 0.75m);
            decimal iqr = q3 - q1;
            decimal low = q1 - IqrMultiplier * iqr;
            decimal high = q3 + IqrMultiplier * iqr;

            var last = group.OrderByDescending(c => c.Period).First();
            if (last.VariationPct < low || last.VariationPct > high)
            {
                anomalies.Add(new AnomalyReport
                {
                    Region = group.Key.Region,
                    Indicator = group.Key.Indicator,
                    ObservedVariationPct = Math.Round(last.VariationPct, 1),
                    HistoricalLowPct = Math.Round(low, 1),
                    HistoricalHighPct = Math.Round(high, 1),
                    Status = "Review recommended"
                });
            }
        }
        return anomalies;
    }

    private static decimal Quantile(List<decimal> sorted, decimal q)
    {
        decimal position = q * (sorted.Count - 1);
        int lower = (int)Math.Floor(position);
        int upper = Math.Min(lower + 1, sorted.Count - 1);
        decimal fraction = position - lower;
        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }

    /// <summary>Candidate à l'analyse : variation observée pour une région/indicateur/période.</summary>
    public class AnomalyCandidate
    {
        public string Region { get; set; } = "";
        public string Indicator { get; set; } = "";
        public string Period { get; set; } = "";
        public decimal VariationPct { get; set; }
    }
}
