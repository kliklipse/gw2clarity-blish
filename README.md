# GW2Clarity Blish

Portage du module GW2Clarity pour [Blish HUD](https://blishhud.com/), l'overlay Guild Wars 2. Affiche des grilles configurables de suivi de buffs pour raid/strike, avec seuils visuels par style (couleur, forme, curseur). Dépend d'ArcDPS Bridge pour recevoir l'état des buffs en temps réel.

Voir la spec et le plan détaillé dans le repo `forge-app` pour le contexte complet (modèles, services, rendu GPU, vues de configuration).

## Architecture des projets

- **`GW2ClarityBlish.csproj`** (racine, `netstandard2.0`) — Models + Services, logique pure, testable. Compatible à la fois avec les tests xUnit (`net8.0`) et le host de module réel (`net472`).
- **`Tests/GW2ClarityBlish.Tests/`** (`net8.0`) — tests xUnit du projet racine.
- **`Module/GW2ClarityBlish.Module.csproj`** (`net472`) — host réel du module Blish HUD, référence le package NuGet public `BlishHUD` (1.3.0) + le projet racine. Contient `Module.cs`, le rendu GPU/shader et les vues de configuration. `manifest.json` vit ici (requis par le build target `BlishHUD.targets` du SDK).

**Note :** `BlishHUD` est un vrai package NuGet public (nuget.org) ciblant `.NETFramework4.7.2` — le Module-Template officiel utilise encore un vieux style `packages.config`, mais le SDK-style `PackageReference` fonctionne tout aussi bien.

## Build

Un quirk MSBuild connu empêche `dotnet build`/`dotnet test` à la racine de la solution de fonctionner proprement quand on mélange `net472`+`netstandard2.0`+`net8.0` dans un même `.sln` (collision de génération d'`AssemblyInfo.cs` sur le projet racine référencé deux fois). Builder/tester **par projet** :

```bash
dotnet build GW2ClarityBlish.csproj                                  # lib Models/Services
dotnet test Tests/GW2ClarityBlish.Tests/GW2ClarityBlish.Tests.csproj # tests xUnit
dotnet build Module/GW2ClarityBlish.Module.csproj                    # module Blish HUD reel (.bhm)
```
