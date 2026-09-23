# Pipeline ETL

Parcours complet d'une donnée, étape par étape :

1. **EmploiVision reçoit le fichier** (upload via la page Imports).
2. Le backend C# **lit le CSV** (`CsvIngestionService.Extract`), en conservant les numéros de ligne.
3. **Validation structurelle** : l'entête doit correspondre au format du type de dataset.
4. **Validation métier** (`ValidationService`) : champs obligatoires, types numériques,
   bornes (année, mois, effectifs, salaires, taux), codes connus dans les référentiels,
   doublons au sein du lot (clé = région + profession + industrie + période).
5. Les lignes incorrectes sont **journalisées** dans `ImportRowError` avec ligne,
   colonne, valeur attendue et valeur reçue : l'administrateur voit exactement pourquoi.
6. Les lignes valides sont **transformées** (`TransformService`) en observations.
7. Les observations sont **chargées en staging** (`SqlBulkCopy` vers `StagingLabourObservation`).
8. La procédure stockée **`usp_LoadStagingToFact`** résout les clés des dimensions et
   fusionne dans `FactLabourMarket` (MERGE : insertion ou mise à jour).
9. Le staging est vidé et l'**import est historisé** (`ImportRun` : durées, comptes, statut).
10. Les KPI, graphiques et le rapport Power BI reflètent le nouvel entrepôt.

## Règle de statut

- `SUCCESS` : aucune ligne rejetée.
- `WARNING` : au moins une ligne rejetée, l'import continue.
- `FAILED` : erreur bloquante (entête invalide, SQL Server injoignable).

## Exemple d'erreur produite

```
Import #428 — ligne 428, colonne EmploymentCount :
attendu Integer, reçu « ABC » — REJECTED
```

## Journalisation

Chaque `ImportRun` conserve : heure de début, heure de fin, lignes traitées /
acceptées / rejetées / avertissements, statut, et la liste des erreurs ligne par ligne.
