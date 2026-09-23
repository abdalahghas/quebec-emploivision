# Schéma de base de données (schéma en étoile)

```
                 DimDate
                    │
DimRegion ─── FactLabourMarket ─── DimProfession
                    │
              DimIndustry
```

## Tables

- **DimRegion** : les 17 régions administratives du Québec (codes et noms officiels).
- **DimProfession** : professions (codes CNP 2021).
- **DimIndustry** : industries (codes SCIAN).
- **DimDate** : une ligne par année/mois, `DateId = AAAAMM` (ex. 202506).
- **FactLabourMarket** : observations quantitatives (`EmploymentCount`,
  `VacancyCount`, `AverageWage`, `UnemploymentRate`) + clés des dimensions et
  `ImportRunId` de traçabilité. Contrainte d'unicité sur la clé d'observation.
- **StagingLabourObservation** : zone d'atterrissage des lignes validées,
  vidée après chaque chargement.
- **ImportRun** / **ImportRowError** : historique des pipelines et erreurs détaillées.

## Vues

- **vw_FactDetails** : jointure étoile complète, utilisée par les KPI du dashboard.
- **vw_PowerBI_Base** : dénormalisation plate prête pour le rapport Power BI.

## Procédures

- **usp_LoadStagingToFact** : résout les clés de dimensions et fusionne le staging
  dans la table de faits (MERGE idempotent sur la clé d'observation).

## Exemple de requête analytique utilisée par le dashboard

```sql
SELECT r.RegionName, d.[Year], SUM(f.EmploymentCount) AS TotalEmployment
FROM FactLabourMarket f
INNER JOIN DimRegion r ON r.RegionId = f.RegionId
INNER JOIN DimDate  d ON d.DateId  = f.DateId
GROUP BY r.RegionName, d.[Year]
ORDER BY d.[Year];
```

## Notions SQL Server mobilisées

`CREATE TABLE`, contraintes `FOREIGN KEY`/`UNIQUE`, `IDENTITY`, `INDEX` avec
`INCLUDE`, vues, procédures stockées, `MERGE`, `CTE` (dans le calcul de variation
annuelle), `GROUP BY`, `ORDER BY`, `NULLIF`, fonctions de date.
