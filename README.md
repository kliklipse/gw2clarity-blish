# GW2Clarity Blish

Portage du module GW2Clarity pour [Blish HUD](https://blishhud.com/), l'overlay Guild Wars 2. Affiche des grilles configurables de suivi de buffs pour raid/strike, avec seuils visuels par style (couleur, forme, curseur). Dépend d'ArcDPS Bridge pour recevoir l'état des buffs en temps réel.

Voir la spec et le plan détaillé dans le repo `forge-app` pour le contexte complet (modèles, services, rendu GPU, vues de configuration).

## Note technique

**Blish HUD SDK reference : à ajouter en Tâche 11 (Module.cs).** Volontairement absente de `GW2ClarityBlish.csproj` pour l'instant — ce SDK n'est pas un package NuGet public standard, il vient de l'installation locale de Blish HUD (absente de cette machine de dev). Les Tâches 2 à 9 (Models/Services) n'ont besoin que de la stdlib .NET (`System.Numerics`, `System.Text.Json`).
