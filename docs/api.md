# API

Toutes les routes sont préfixées par `/api`. Les réponses sont en JSON.

| Méthode | Route | Rôle |
| --- | --- | --- |
| GET | `/api/dashboard` | KPI et séries historiques. Paramètres : `region`, `industry`, `year` |
| GET | `/api/regions` | Liste des régions (dimension) |
| GET | `/api/regions/{name}` | Fiche d'une région : indicateurs + historique |
| GET | `/api/industries` | Fiches industries avec agrégats |
| GET | `/api/professions?search=&year=` | Fiches professions, filtrables |
| GET | `/api/comparison?left=&right=&year=` | Comparaison de deux régions |
| GET | `/api/forecast?region=&industry=&horizon=` | Prévision par tendance linéaire |
| GET | `/api/data-quality` | Score de qualité + anomalies IQR |
| GET | `/api/imports` | Historique des imports |
| GET | `/api/imports/{id}` | Détail d'un import avec erreurs ligne par ligne |
| GET | `/api/pipeline` | Santé des sources + dernier pipeline |
| POST | `/api/import` | Import ETL d'un CSV (multipart : `dataset`, `file`) |

## Comportements d'erreur

- SQL Server injoignable → `503` avec `{ "message": "Impossible de récupérer les données. Réessayez plus tard." }`
- Paramètres manquants ou invalides → `400` avec message explicite.
- Import introuvable → `404`.
- Horizon de prévision hors bornes (1 à 24) → `400`.

## Formats CSV attendus

```
EmploymentByRegionIndustry : RegionCode,IndustryCode,Year,Month,EmploymentCount
VacanciesByRegion           : RegionCode,Year,Month,VacancyCount
WagesByProfession           : RegionCode,ProfessionCode,Year,Month,AverageWage
```

`Month` vide = valeur annuelle (le pipeline l'interprète comme janvier pour la
clé de dimension, ce qui reste cohérent avec les agrégations annuelles).
