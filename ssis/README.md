# Démonstration SSIS

Un petit package SSIS complète le pipeline applicatif en montrant l'outillage
Microsoft classique d'intégration. À créer dans Visual Studio (extension
SQL Server Integration Services) : le fichier .dtsx n'est pas reconstruit ici,
sa conception est documentée.

## Flux du package (Package.dtsx)

```
data/employment_by_region_industry.csv
            │
     Flat File Source
            │
     Data Conversion        (Year/Month/EmploymentCount → types SQL)
            │
     Conditional Split
        ↙          ↘
  Lignes valides   Lignes invalides
       │                │
  OLE DB Destination   Flat File Destination (erreurs.csv)
       │
  Table : StagingLabourObservation
```

## Étapes dans Visual Studio

1. Nouveau projet → **Integration Services Project**.
2. `Flat File Source` : colonnes délimitées, séparateur virgule, entête sur la ligne 1.
3. `Data Conversion` : `Year`/`Month`/`EmploymentCount` en DT_I4.
4. `Conditional Split` : condition `!ISNULL(EmploymentCount) && Year > 2000`,
   sortie `Invalid` sinon.
5. `OLE DB Destination` vers `StagingLabourObservation` (le même staging que
   le pipeline applicatif).
6. Exécuter le package : il alimente le même entrepôt, et le dashboard ASP.NET
   reflète le résultat immédiatement.

## Rôle dans le projet

SSIS n'est **pas** le cœur du projet : le pipeline applicatif C# couvre déjà
Extract → Validate → Transform → Load. Le package montre la même capacité avec
l'outil Microsoft dédié, utile à mentionner quand l'offre demande des notions SSIS.
