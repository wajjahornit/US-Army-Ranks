# ArmyRanks (SPT 4.1.x)

> Thank you for downloading this mod, have a blessed day!

Shows U.S. Army ranks (PV1 / E-1 up to GA / O-11) in place of your player level, levels 1–79.

It has two parts, because the game client draws the level number itself:

| Folder | What it is | What it changes |
|---|---|---|
| `ArmyRanks/` | SPT **server** mod (C#, .NET 10) | Renames the words around your level in the English text: "Level" becomes "Rank", "Experience needed for next level" becomes "…next promotion", and so on. |
| `ArmyRanksClient/` | BepInEx **client** plugin (C#, .NET Framework 4.7.2) | Replaces the level number itself with your rank: on the level badge (character screen, experience bar, post-raid screen, matchmaker, party list) and on the trader screen. |
| `Shared/ArmyRankTable.cs` | The 79-level rank table | Compiled into both, so they always agree. |

Either part works alone. Use both for the full effect.

## Why SPT 4.x needs C#

SPT 4.0 rewrote the server from TypeScript/Node.js to C#/.NET, so `package.json` + `src/mod.ts` mods no longer load. In 4.x:

- `ArmyRanks.csproj` does the job of `package.json` (build settings)
- the `ModMetadata` record in `ArmyRanksMod.cs` holds the name, author, version and `SptVersion`
- `[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` + `IOnLoad` replaces `IPostDBLoadMod.postDBLoad`

## Build

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
# server mod: pulls the SPT libraries from NuGet
dotnet build ArmyRanks/ArmyRanks.csproj -c Release

# client plugin: point EftDir at your SPT folder (the one with EscapeFromTarkov.exe)
dotnet build ArmyRanksClient/ArmyRanksClient.csproj -c Release -p:EftDir="C:\SPT"
```

## Install (release zip)

Extract the release zip (e.g. `ArmyRanks-1.0.1.zip`) into your SPT game folder, the one that has `EscapeFromTarkov.exe` and the `BepInEx` folder. It contains `BepInEx\plugins\ArmyRanksClient.dll` and `SPT_Runtime\user\mods\ArmyRanks\ArmyRanks.dll`. If your server's mods folder is somewhere else, move the `ArmyRanks` folder into that `user\mods`.

## Install (from source)

1. Server: copy the `ArmyRanks/bin/Release/ArmyRanks/` folder into your server's `user\mods\` folder (for example `C:\SPT\SPT_Runtime\user\mods\ArmyRanks\ArmyRanks.dll`). The folder must contain the built DLL, not the source files.
2. Client: copy `ArmyRanksClient/bin/Release/net472/ArmyRanksClient.dll` into `SPT\BepInEx\plugins\`.
3. Start the server. You should see `[ArmyRanks] Relabelled 9 level labels…` in the console.

## Options

The client creates `BepInEx\config\com.cotti.armyranks.client.cfg` (also editable in-game with F12 if you have Configuration Manager):

- `FullTitle` (default): `Staff Sergeant`
- `FullTitleWithGrade`: `Staff Sergeant (SSG / E-6)`
- `Abbreviation`: `SSG`
- `PayGrade`: `E-6`
- `AbbreviationAndPayGrade`: `SSG E-6`

Long rank names stack onto two lines ("Master / Sergeant") so they stay compact next to the badge icon.

## MoxoPixel MenuOverhaul

If MenuOverhaul is installed, the plugin also hooks its main-menu level display (`PlayerProfileStatsController.UpdateLevelDisplay`), so the big number next to your name shows your rank. Without MenuOverhaul this hook just switches off.

## What still shows a number

- Quest and map requirements ("Character rank: 15", "Unlocks at level 20"). These are text templates the game fills with a number, and there are many of them.
- Other players' levels on the post-raid kill list.
- The trader screen's loyalty table (your level vs. the level each loyalty tier needs). Only its icon changes.
- Non-English languages: the server mod only relabels `en`.

## Rank emblems

The game's level chevron is replaced with the U.S. Army insignia for your rank (`ArmyRanksClient/Icons/<abbreviation>.png`, built into the DLL). To change one, replace its PNG and rebuild. All 28 ranks have an emblem. Private (PV1) has no insignia in the real Army, so it uses the PV2 chevron.
