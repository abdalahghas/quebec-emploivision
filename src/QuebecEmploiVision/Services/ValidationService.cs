using QuebecEmploiVision.Models;

namespace QuebecEmploiVision.Services;

/// <summary>
/// VALIDATION métier : chaque règle est explicite et testable.
///   - valeurs obligatoires présentes;
///   - types numériques valides (délégués au CsvIngestionService);
///   - valeurs impossibles (année hors bornes, effectif négatif, salaire négatif);
///   - codes de région/profession/industrie existant dans les référentiels;
///   - doublons au sein du même lot (même clé d'observation).
/// Les erreurs bloquent la ligne ; les avertissements l'acceptent mais sont journalisés.
/// </summary>
public class ValidationService
{
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    private readonly HashSet<string> _regionCodes;
    private readonly HashSet<string> _professionCodes;
    private readonly HashSet<string> _industryCodes;

    public ValidationService(IEnumerable<string> regionCodes,
                             IEnumerable<string> professionCodes,
                             IEnumerable<string> industryCodes)
    {
        _regionCodes = new HashSet<string>(regionCodes, StringComparer.OrdinalIgnoreCase);
        _professionCodes = new HashSet<string>(professionCodes, StringComparer.OrdinalIgnoreCase);
        _industryCodes = new HashSet<string>(industryCodes, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Valide une ligne déjà transformée en observation. Retourne vrai si acceptable.</summary>
    public bool Validate(StagedObservation observation, RawRow source)
    {
        bool acceptable = source.Errors.All(e => e.Severity != "ERROR");

        if (observation.RegionCode.Length == 0)
        {
            Add(source, "RegionCode", "Valeur obligatoire", "");
        }
        else if (!_regionCodes.Contains(observation.RegionCode))
        {
            Add(source, "RegionCode", "Code de région connu", observation.RegionCode);
        }

        if (observation.ProfessionCode.Length > 0 && !_professionCodes.Contains(observation.ProfessionCode))
        {
            Add(source, "ProfessionCode", "Code CNP connu", observation.ProfessionCode);
        }

        if (observation.IndustryCode.Length > 0 && !_industryCodes.Contains(observation.IndustryCode))
        {
            Add(source, "IndustryCode", "Code SCIAN connu", observation.IndustryCode);
        }

        if (observation.Year < MinYear || observation.Year > MaxYear)
        {
            Add(source, "Year", $"Année entre {MinYear} et {MaxYear}", observation.Year.ToString());
        }

        if (observation.Month is < 1 or > 12)
        {
            Add(source, "Month", "Mois entre 1 et 12", observation.Month?.ToString() ?? "");
        }

        if (observation.EmploymentCount < 0)
        {
            Add(source, "EmploymentCount", "Entier >= 0", observation.EmploymentCount?.ToString() ?? "");
        }

        if (observation.VacancyCount < 0)
        {
            Add(source, "VacancyCount", "Entier >= 0", observation.VacancyCount?.ToString() ?? "");
        }

        if (observation.AverageWage < 0)
        {
            Add(source, "AverageWage", "Décimal >= 0", observation.AverageWage?.ToString() ?? "");
        }

        if (observation.UnemploymentRate is < 0 or > 100)
        {
            Add(source, "UnemploymentRate", "Pourcentage entre 0 et 100",
                observation.UnemploymentRate?.ToString() ?? "");
        }

        return acceptable && source.Errors.All(e => e.Severity != "ERROR");
    }

    /// <summary>Détecte les doublons dans un lot (même clé d'observation).</summary>
    public void DetectDuplicates(IReadOnlyList<(StagedObservation Observation, RawRow Source)> batch)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (observation, source) in batch)
        {
            var key = ObservationKey(observation);
            if (!seen.Add(key))
            {
                source.Errors.Add(new ImportRowError
                {
                    RowNumber = source.RowNumber,
                    ColumnName = "(clé)",
                    Expected = "Observation unique",
                    Received = key,
                    Severity = "ERROR",
                    Message = "Doublon : la même observation figure déjà dans ce lot."
                });
            }
        }
    }

    /// <summary>Clé d'unicité d'une observation : région + profession + industrie + période.</summary>
    public static string ObservationKey(StagedObservation o) =>
        $"{o.RegionCode}|{o.ProfessionCode}|{o.IndustryCode}|{o.Year}-{o.Month ?? 0}";

    private void Add(RawRow source, string column, string expected, string received)
    {
        source.Errors.Add(new ImportRowError
        {
            RowNumber = source.RowNumber,
            ColumnName = column,
            Expected = expected,
            Received = received,
            Severity = "ERROR",
            Message = $"Valeur invalide dans {column} : attendu {expected}, reçu « {received} »."
        });
    }
}
