# Tests

```bash
cd src && dotnet test
```

**31 tests unitaires xUnit**, tous indépendants de la base de données :

## DataValidationTests (11)

- accepte un enregistrement valide;
- rejette un entier non numérique (`ABC`);
- rejette une région manquante ou inconnue;
- rejette une année impossible, un mois hors bornes;
- rejette un effectif négatif, un salaire négatif, un taux de chômage hors 0–100;
- détecte un doublon dans le lot;
- la clé d'unicité distingue les périodes.

## ImportPipelineTests (8)

- l'extraction ignore l'entête et les lignes vides, en conservant les numéros de ligne;
- l'entête attendue est reconnue, un mauvais format est refusé;
- les transformations mappent correctement les colonnes (emplois et salaires);
- une ligne tronquée produit une erreur propre, pas une exception;
- un pipeline complet sur fichier valide accepte tout;
- un pipeline complet sur le CSV d'erreurs de démonstration rejette exactement
  les 4 lignes fautives avec les bons messages.

## ForecastServiceTests (5)

- une tendance linéaire exacte est projetée correctement (R² = 1);
- des données bruitées donnent un R² faible;
- moins de 3 observations → pas de prévision;
- les labels annuels et mensuels avancent correctement;
- la méthodologie et ses limites sont exposées avec le résultat.

## AnomalyServiceTests (3)

- une variation extrême est signalée comme « à vérifier »;
- une variation normale n'est pas signalée;
- un historique trop court est ignoré.

## DataQualityServiceTests (4)

- import parfait → 100/100;
- les rejets dégradent le score proprement;
- entrée vide → 0 sans division par zéro;
- le score est explicable composante par composante.

Un test a d'ailleurs intercepté un vrai bug pendant le développement : une ligne
CSV tronquée provoquait une `IndexOutOfRangeException` au lieu d'un rejet propre.
C'est exactement le rôle de ces tests.
