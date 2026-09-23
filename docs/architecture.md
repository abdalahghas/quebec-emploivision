# Architecture

```
DONNÉES PUBLIQUES (CSV / API)
        │
        ▼
   INGESTION (CsvIngestionService)          ← EXTRACT
        │
        ▼
   STAGING SQL (StagingLabourObservation)
        │
        ▼
   VALIDATION (ValidationService)            ← règles métier, référentiels, doublons
        │
        ▼
   TRANSFORMATION (TransformService)         ← mapping colonnes → observation
        │
        ▼
   DATA WAREHOUSE (schéma en étoile)         ← LOAD (MERGE via usp_LoadStagingToFact)
        │
   ┌────┴────┐
   ▼         ▼
ASP.NET    POWER BI
   │
   ▼
DASHBOARD WEB (KPI, filtres, comparateur, prévisions, qualité)
```

## Couches C#

| Couche | Dossier | Rôle |
| --- | --- | --- |
| Contrôleurs | `Controllers/` | Reçoivent les requêtes HTTP, valident les entrées, retournent des erreurs lisibles |
| Services | `Services/` | Toute la logique : ETL, validation, prévision, anomalies, qualité, requêtes analytiques |
| Modèles | `Models/` | Entités du domaine (observation, import, erreur) |
| DTOs | `DTOs/` | Objets transportés vers l'API (KPI, séries, comparaisons, scores) |
| Data | `Data/` | Accès ADO.NET minimal à SQL Server |

## Décisions d'architecture

- **ADO.NET plutôt qu'un ORM** : le SQL est le cœur du projet; chaque requête
  reste visible et explicable, et le schéma en étoile vit dans des scripts versionnés.
- **Pas de Clean Architecture à 46 projets** : une structure pédagogique, propre,
  où chaque fichier tient dans la tête.
- **Idempotence des imports** : le `MERGE` sur la clé d'observation rend un
  ré-import sans effet de bord (pas de doublons dans la table de faits).
- **Séparation pur/logique** : validation, prévision, anomalies et score de
  qualité sont des classes sans dépendance à la base, donc testables unitairement.
