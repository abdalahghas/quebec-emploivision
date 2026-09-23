# Installation

## Prérequis

- **Visual Studio 2022** (charge de travail ASP.NET et développement Web) ou SDK .NET 8
- **SQL Server 2022** (Developer, Express ou LocalDB suffisent)
- Optionnel : SQL Server Management Studio (SSMS)

## 1. Base de données

Exécuter dans SSMS, dans l'ordre :

```
database/01_create_database.sql
database/02_star_schema.sql
database/03_seed_dimensions.sql
```

Les scripts sont idempotents : ils peuvent être relancés sans danger.

## 2. Chaîne de connexion

`src/QuebecEmploiVision/appsettings.json` :

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EmploiVision;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Avec LocalDB : `Server=(localdb)\\MSSQLLocalDB;Database=EmploiVision;Trusted_Connection=True;`

## 3. Lancer l'application

```bash
cd src
dotnet run --project QuebecEmploiVision
```

Le dashboard s'ouvre sur http://localhost:5000 (ou le port indiqué dans la console).

## 4. Charger les données (démonstration)

Ouvrir la page **Imports** du dashboard et importer successivement les trois CSV de `data/` avec leur type correspondant :

| Fichier | Type de dataset |
| --- | --- |
| `data/employment_by_region_industry.csv` | EmploymentByRegionIndustry |
| `data/vacancies_by_region.csv` | VacanciesByRegion |
| `data/wages_by_profession.csv` | WagesByProfession |

Chaque import passe par le pipeline complet : Extract → Validate → Transform → Staging → Load.
`data/demo_import_with_errors.csv` montre le rejet ligne par ligne (région inconnue,
mois non numérique, année impossible, doublon) : c'est le scénario de démonstration en entrevue.

## 5. Tests

```bash
cd src
dotnet test
```

31 tests unitaires couvrent la validation, le pipeline ETL, le modèle de prévision,
la détection d'anomalies et le score de qualité — sans base de données requise.

## Sources de données réelles

Les CSV de `data/` sont des échantillons de démonstration au format d'import, générés
avec des ordres de grandeur plausibles. Pour des données réelles, télécharger depuis
Statistique Canada (Tableau 14-10-0090-01, caractéristiques de la population active
par région économique, et tableaux connexes) et aligner les colonnes sur le format
attendu par le pipeline. Voir `docs/data-sources.md`.
