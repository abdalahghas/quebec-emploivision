# Sources de données

## Échantillons livrés

Les CSV de `data/` sont des **échantillons de démonstration** au format d'import.
Ils utilisent les régions administratives réelles du Québec (17 régions, codes
officiels), des professions CNP 2021 et des industries SCIAN réelles, avec des
ordres de grandeur plausibles (pondération par région, creux COVID en 2020 pour
les services, progression salariale ~2 % par an). **Ce ne sont pas des chiffres
officiels** : ils servent la démonstration du pipeline, pas l'analyse réelle.

## Sources publiques réelles à utiliser

- **Statistique Canada**, tableau **14-10-0090-01** : caractéristiques de la
  population active par région économique (emploi, chômage) —
  https://www150.statcan.gc.ca/
- **Statistique Canada**, tableau **14-10-0355-01** : postes vacants selon la
  région et le secteur.
- **Statistique Canada, EPVS** : Enquête sur les postes vacants et les
  salaires, dont les thèmes (postes vacants par région, salaires par profession)
  ont inspiré le périmètre de données de ce projet.
- **Institut de la statistique du Québec (ISQ)** : enquêtes et statistiques du
  marché du travail — https://www.stat.gouv.qc.ca/
- **Guichet emplois / emploi Québec** : offres et perspectives par profession.

Les tableaux Statistique Canada se téléchargent en CSV : il suffit d'aligner les
colonnes sur le format attendu par le pipeline (voir `docs/api.md`) pour importer
des données réelles.

## Métadonnées suivies par import

Chaque exécution enregistre : nom du dataset, nom du fichier source, dates de
début/fin, lignes traitées/acceptées/rejetées/avertissements et statut. L'entrepôt
identifie, pour chaque observation, l'import qui l'a chargée (`ImportRunId`).
