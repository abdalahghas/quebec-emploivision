# Limites assumées

## Données

- Les CSV de démonstration sont des échantillons générés avec des ordres de
  grandeur plausibles, **pas des chiffres officiels**. La provenance réelle doit
  être téléchargée depuis Statistique Canada / l'ISQ (voir `data-sources.md`).
- Tous les indicateurs ne sont pas disponibles à la même granularité dans les
  sources publiques : l'entrepôt garde des clés de dimensions nullables
  (profession/industrie) plutôt que de forcer des données incompatibles dans une
  seule table.

## Modèle de prévision

- La prévision est une **tendance linéaire** : elle extrapole le passé, ignore
  les chocs économiques, la saisonnalité et les cycles. Elle n'est pas un modèle
  économétrique. Les limites sont affichées avec le résultat (R², pente, méthodologie).

## Détection d'anomalies

- L'IQR signale des observations **inhabituelles**, jamais « fausses » : chaque
  anomalie requiert une vérification humaine.

## Portée applicative

- Deux rôles (analyste, administrateur) sans système de permissions complet :
  l'import est ouvert dans cette version de démonstration.
- Pas d'authentification, pas d'exports Excel/PDF, pas de favoris —
  volontairement hors périmètre de la première version.

## Power BI / SSIS

- Le rapport Power BI et le package SSIS sont **documentés et conçus**
  (`powerbi/`, `ssis/`) mais ne peuvent pas être inclus comme fichiers binaires
  dans le dépôt : leur création dans Power BI Desktop / Visual Studio est la
  partie que l'on fait soi-même sur son poste.
