# Québec EmploiVision

Plateforme Web d'intégration, d'analyse et de prévision du marché du travail québécois : un pipeline de données complet, du CSV public au dashboard.

**ASP.NET Core (C#) · SQL Server (schéma en étoile) · ETL · Prévisions statistiques · Détection d'anomalies · Power BI · SSIS**

## Le problème résolu

Les données publiques du marché du travail arrivent en CSV, à des dates différentes, avec des erreurs. Répéter sans fin *télécharger → ouvrir Excel → nettoyer → recalculer → refaire les graphiques* est long et fragile. EmploiVision automatise cette chaîne :

```
DONNÉES PUBLIQUES  →  INGESTION  →  STAGING SQL  →  VALIDATION
       →  TRANSFORMATION  →  DATA WAREHOUSE (SQL Server)
              →  ASP.NET (dashboard)   +   POWER BI (rapport)
```

Le système répond à : comment l'emploi évolue à Montréal, quelles professions varient le plus, comment les salaires évoluent, quelle région a le plus de postes vacants, et ce qu'une tendance statistique simple projette pour les prochaines périodes.

## Inspiration : la production de statistiques officielles

Le périmètre de données (postes vacants, salaires par profession, emploi par
région et par industrie) reprend les thèmes d'enquêtes officielles comme
l'Enquête sur les postes vacants et les salaires (EPVS) de Statistique Canada,
et le pipeline reflète les mêmes étapes que la production de statistiques du
marché du travail : collecte, validation, transformation, entrepôt de données,
puis diffusion d'indicateurs fiables. C'est un exercice personnel d'ingénierie
de données inspiré de ces processus, pas une reproduction de l'EPVS.

## Fonctionnalités

- **Vue globale** : KPI (emplois, postes vacants, salaire moyen, variation annuelle) générés depuis SQL Server, jamais codés en dur, avec filtres région/industrie/année partagés par toute l'application.
- **Emploi / Professions / Industries / Régions** : évolutions historiques, fiches détaillées, recherche.
- **Comparateur** : deux régions côte à côte, métrique par métrique, avec historique comparé.
- **Prévisions** : tendance linéaire (moindres carrés) avec R², pente, horizon réglable et limites de la méthode affichées avec le résultat.
- **Data Quality Center** : score documenté (complétude, validité, unicité, cohérence) + détection d'anomalies par IQR, formulée comme « à vérifier », jamais comme « fausse ».
- **Imports** : upload CSV avec rejet ligne par ligne (ligne, colonne, attendu, reçu) et historique complet des pipelines.
- **Pipeline Monitor** : santé des sources, compteurs, statut du dernier import.
- Mode sombre, responsive, gestion d'erreurs partout (jamais de page blanche).

## Stack

| Couche | Technologie |
| --- | --- |
| Backend | C# / ASP.NET Core 8 (contrôleurs + services, ADO.NET) |
| Base de données | Microsoft SQL Server (schéma en étoile, vues, procédure stockée, MERGE) |
| Frontend | HTML/CSS/JS + Chart.js (sobre, type outil gouvernemental) |
| Tests | xUnit (31 tests unitaires) |
| BI | Power BI (rapport 3 pages documenté) + flux Power Automate |
| ETL outil | Démonstration SSIS documentée |

## Démarrage rapide

```bash
# 1. Base de données : exécuter database/01..03_*.sql dans SSMS
# 2. Lancer l'application
cd src
dotnet run --project QuebecEmploiVision
# 3. Importer data/*.csv depuis la page Imports du dashboard
# 4. Tests
dotnet test
```

Détails : [docs/installation.md](docs/installation.md)

## Structure du dépôt

```
src/QuebecEmploiVision/        Application ASP.NET Core (contrôleurs, services, DTOs, wwwroot)
src/QuebecEmploiVision.Tests/   31 tests xUnit (validation, ETL, prévision, anomalies, qualité)
database/                       Scripts SQL Server (création, schéma en étoile, dimensions)
data/                           CSV de démonstration, dont un fichier avec erreurs volontaires
docs/                           Architecture, ETL, API, base, tests, limites, installation
powerbi/                        Rapport Power BI : connexion, pages, mesures DAX
ssis/                           Package SSIS : conception documentée
```

## Qualité vérifiable

- **31 tests unitaires verts** (validation métier, pipeline complet sur fichiers valides
  et fautifs, régression linéaire, IQR, score de qualité) — un de ces tests a intercepté
  un vrai bug pendant le développement (ligne CSV tronquée → exception au lieu d'un rejet propre).
- Le projet compile sans erreur et sans avertissement de nullabilité sur .NET 8.
- Les erreurs utilisateurs sont explicites : « Impossible de récupérer les données », « Import interrompu — N erreurs bloquantes », « Aucune observation disponible pour cette combinaison de filtres ».

## Ce que le projet démontre en entrevue

1. Ouvrir le dashboard, filtrer sur Montréal, comparer deux régions.
2. Importer un CSV dans la page Imports : Extract → Validate → Transform → Load, visibles dans le Pipeline Monitor.
3. Importer `demo_import_with_errors.csv` : 4 rejets documentés ligne par ligne (région inconnue, mois non numérique, année impossible, doublon).
4. Montrer le score de qualité et une anomalie IQR.
5. Montrer la prévision avec ses limites.
6. Ouvrir le code : chaque couche tient dans la tête, chaque requête SQL est visible, chaque règle de validation a un test.

## Limites assumées

Les CSV de démonstration sont des échantillons à ordres de grandeur plausibles, pas des chiffres officiels (les sources réelles sont documentées). La prévision est une tendance linéaire qui n'anticipe pas les chocs économiques. Pas d'authentification ni de permissions fines dans cette version. Détail complet : [docs/limitations.md](docs/limitations.md)
