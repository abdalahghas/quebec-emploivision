using QuebecEmploiVision.DTOs;

namespace QuebecEmploiVision.Services;

/// <summary>
/// Score de qualité documenté, sans magie : chaque composante correspond à des
/// règles mesurables sur le dernier import.
///   Completeness : champs obligatoires réellement renseignés;
///   Validity      : lignes respectant types et bornes attendus;
///   Uniqueness    : observations non dupliquées;
///   Consistency   : clés étrangères résolues vers les référentiels.
/// </summary>
public class DataQualityService
{
    public QualityScore Score(QualityInputs inputs)
    {
        decimal completeness = Ratio(inputs.RequiredCellsPresent, inputs.RequiredCellsTotal);
        decimal validity = inputs.RowsTotal == 0 ? 0 : Ratio(inputs.RowsTypeValid, inputs.RowsTotal);
        decimal uniqueness = inputs.ObservationsTotal == 0 ? 0
            : Ratio(inputs.ObservationsTotal - inputs.DuplicateRows, inputs.ObservationsTotal);
        decimal consistency = inputs.ObservationsTotal == 0 ? 0
            : Ratio(inputs.ForeignKeysResolved, inputs.ForeignKeysTotal);

        var score = new QualityScore
        {
            Completeness = Round1(completeness * 100),
            Validity = Round1(validity * 100),
            Uniqueness = Round1(uniqueness * 100),
            Consistency = Round1(consistency * 100)
        };
        // Moyenne pondérée simple : les quatre axes pèsent pareil par choix explicite.
        score.Overall = Round1((score.Completeness + score.Validity + score.Uniqueness + score.Consistency) / 4);
        return score;
    }

    private static decimal Ratio(decimal part, decimal total) =>
        total == 0 ? 0 : part / total;

    private static decimal Round1(decimal value) =>
        Math.Round(value, 1);
}

/// <summary>Compteurs bruts mesurés pendant le dernier import.</summary>
public class QualityInputs
{
    public int RowsTotal { get; set; }
    public int RowsTypeValid { get; set; }
    public int RequiredCellsTotal { get; set; }
    public int RequiredCellsPresent { get; set; }
    public int ObservationsTotal { get; set; }
    public int DuplicateRows { get; set; }
    public int ForeignKeysTotal { get; set; }
    public int ForeignKeysResolved { get; set; }
}
