# RimSearcher Project Environment

- Status: Incomplete

## Tool Versions

- Project CLI: 3.1.5
- Database Version: 3.1.5

## Game Locations

- Game Root: E:\SteamLibrary\steamapps\common\RimWorld
- Game DLL Directory: E:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed
- Core Assembly Assembly-CSharp.dll: E:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
- DataMod Installation Directory: E:\SteamLibrary\steamapps\common\RimWorld\Mods\RimSearcher_DataMod

## Dependent Mods

- Mod Directories:
  - Local (project): E:\SteamLibrary\steamapps\common\RimWorld\Mods\东方星缘记
  - Local: E:\SteamLibrary\steamapps\common\RimWorld\Mods\CE Patches
  - Workshop: E:\SteamLibrary\steamapps\workshop\content\294100
- Relevant Assembly DLLs:
  - Core: E:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
  - Project: E:\SteamLibrary\steamapps\common\RimWorld\Mods\东方星缘记\Assemblies

## Def Snapshot

- Project Database: defs.db beside this README (hardlink to DataMod export)
- Export Source Path: E:\SteamLibrary\steamapps\common\RimWorld\Mods\RimSearcher_DataMod\defs.db
- Export Timestamp: 2026-10-01 10:14:55
- Game Version, DLCs, Active Mods & Load Order:
  - Game Version: 1.6.4871 rev590
  - DLCs: Royalty, Ideology, Biotech, Anomaly, Odyssey
  - Active Mods (via `mods`): Core, Ideology, Odyssey, 东方秘星异闻 (touhou.merissu), Biotech, Anomaly, Royalty, Ariandel Library, Vanilla Expanded Framework, Ancot Library, Melee Animation, Character Editor, HugsLib, Dubs Performance Analyzer (+1 Unknown, 1270 defs)

## Notes

- defs.db is a hardlink to the DataMod export (no extra disk usage). If the DataMod re-exports by replacing the file (temp + rename), recreate the hardlink to refresh the snapshot.
- DecompilerServer MCP is NOT configured yet (optional, needed for C# source decompilation / Harmony hook analysis).
