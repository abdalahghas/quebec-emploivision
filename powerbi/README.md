# Rapport Power BI

Le rapport se connecte à l'entrepôt SQL Server et complète le dashboard Web.
À créer dans Power BI Desktop (gratuit) : les fichiers .pbix ne se versionnent
pas proprement, ce dossier documente donc le rapport à reconstituer.

## Connexion

1. Obtenir des données → **SQL Server**.
2. Serveur : `localhost`, base : `EmploiVision`.
3. Mode **Importation** (le volume est faible) ou DirectQuery.
4. Sélectionner la vue **`vw_PowerBI_Base`** : elle expose déjà la jointure
   étoile dénormalisée (région, profession, industrie, année, mois, trimestre,
   indicateurs).

## Pages du rapport

### 1. Executive Overview

- Cartes KPI : emploi total, postes vacants, salaire horaire moyen, variation annuelle.
- Courbe d'évolution de l'emploi par année.
- Segments (slicers) : Année, Région, Industrie.

### 2. Regional Analysis

- Carte/-barres : emploi par région administrative.
- Tableau : régions × indicateurs avec triées par variation.
- Segment région lié aux autres visuels.

### 3. Occupation Analysis

- Barres : professions par emploi.
- Nuage de points : salaire moyen vs emploi par profession.
- Tableau détaillé professions.

## Mesures DAX de départ

```dax
Total Emploi = SUM(vw_PowerBI_Base[EmploymentCount])

Salaire moyen = AVERAGE(vw_PowerBI_Base[AverageWage])

Postes Vacants = SUM(vw_PowerBI_Base[VacancyCount])

Variation Emploi % =
VAR AnneePrecedente =
    CALCULATE([Total Emploi],
             vw_PowerBI_Base[Year] = MAX(vw_PowerBI_Base[Year]) - 1)
RETURN DIVIDE([Total Emploi] - AnneePrecedente, AnneePrecedente)
```

## Power Automate (automatisation de notification)

Flux simple déclenché après un import (via appel HTTP du backend ou planificateur) :

```
NOUVEL IMPORT → SUCCÈS ? → OUI : courriel "EmploiVision — Import terminé"
                          → NON : courriel d'alerte "Import interrompu"
```

Contenu du courriel : dataset, lignes traitées/acceptées/rejetées, durée,
statut. C'est l'exemple minimal d'automatisation réelle mentionné dans le
README principal.
