using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public static class GapEmergencyTeleportUtility
    {
        private static readonly HediffDef ActiveHediff = HediffDef.Named("Turnenemiesintofriends");
        private static readonly SoundDef GapOneSound = SoundDef.Named("gapone");
        private static readonly SoundDef GapTwoSound = SoundDef.Named("gaptwo");

        public static bool IsProtected(Pawn pawn)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                pawn.health == null ||
                pawn.health.hediffSet == null)
                return false;
            return pawn.health.hediffSet.GetFirstHediffOfDef(ActiveHediff) != null;
        }

        public static bool TryEmergencyTeleport(Pawn pawn)
        {
            if (!IsProtected(pawn))
                return false;
            if (!pawn.Spawned || pawn.Map == null)
                return false;
            Map map = pawn.Map;
            IntVec3 startCell = pawn.Position;
            IntVec3 destination;
            if (!TryFindSafeCell(pawn, map, 5f, out destination))
                return false;
            Thing_GapTeleporter teleporter = (Thing_GapTeleporter)ThingMaker.MakeThing(ThingDef.Named("GapTeleporter"));
            teleporter.destCell = destination;
            teleporter.caster = pawn;
            teleporter.stunTicks = 0;
            teleporter.destClamorType = null;
            teleporter.destClamorRadius = 0f;
            teleporter.isPawn = true;
            teleporter.randomSafeDestination = true;
            GenSpawn.Spawn(teleporter, startCell, map);
            if (!teleporter.Spawned)
            {
                teleporter.Destroy();
                return false;
            }
            GapOneSound?.PlayOneShot(new TargetInfo(startCell, map));
            pawn.DeSpawn();
            if (!teleporter.innerContainer.TryAdd(pawn))
            {
                teleporter.Destroy();
                if (!pawn.Destroyed && !pawn.Spawned)
                    GenSpawn.Spawn(pawn, startCell, map, WipeMode.Vanish);
                return false;
            }
            teleporter.CacheTexture();
            GapTwoSound?.PlayOneShot(new TargetInfo(destination, map));
            return true;
        }

        public static bool TryFindSafeCell(
            Pawn pawn,
            Map map,
            float minimumEnemyDistance,
            out IntVec3 destination)
        {
            destination = IntVec3.Invalid;

            if (pawn == null || map == null || !pawn.Spawned)
                return false;

            List<Pawn> enemies = GetHostilePawns(pawn, map);

            int bestDistanceSquared = int.MaxValue;
            bool found = false;

            for (int x = 1; x < map.Size.x - 1; x++)
            {
                for (int z = 1; z < map.Size.z - 1; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);

                    if (!IsSafeCell(
                        pawn,
                        map,
                        cell,
                        minimumEnemyDistance,
                        enemies))
                    {
                        continue;
                    }

                    int dx = cell.x - pawn.Position.x;
                    int dz = cell.z - pawn.Position.z;

                    int distanceSquared = dx * dx + dz * dz;

                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        destination = cell;
                        found = true;
                    }
                }
            }

            return found;
        }
        public static bool IsSafeArrivalCell(
            Pawn pawn,
            Map map,
            IntVec3 cell,
            float minimumEnemyDistance)
        {
            if (pawn == null || map == null)
                return false;
            List<Pawn> enemies = GetHostilePawns(pawn, map);
            return IsSafeCell(pawn, map, cell, minimumEnemyDistance, enemies);
        }

        private static List<Pawn> GetHostilePawns(Pawn pawn, Map map)
        {
            List<Pawn> enemies = new List<Pawn>();
            if (pawn.Faction == null)
                return enemies;
            IReadOnlyList<Pawn> spawnedPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn other = spawnedPawns[i];
                if (other == null ||
                    other == pawn ||
                    other.Dead ||
                    other.Destroyed ||
                    other.Faction == null)
                    continue;
                if (pawn.Faction.HostileTo(other.Faction))
                    enemies.Add(other);
            }
            return enemies;
        }

        private static bool IsSafeCell(
            Pawn pawn,
            Map map,
            IntVec3 cell,
            float minimumEnemyDistance,
            List<Pawn> enemies)
        {
            if (!cell.InBounds(map))
                return false;
            if (!cell.Standable(map))
                return false;
            if (cell == pawn.Position)
                return false;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null)
                    continue;
                Pawn otherPawn = thing as Pawn;
                if (otherPawn != null &&
                    otherPawn != pawn &&
                    !otherPawn.Dead &&
                    !otherPawn.Destroyed)
                    return false;
                if (thing.def == ThingDefOf.Fire)
                    return false;
            }
            if (minimumEnemyDistance > 0f)
            {
                float minDistanceSquared = minimumEnemyDistance * minimumEnemyDistance;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Pawn enemy = enemies[i];
                    if (!enemy.Spawned)
                        continue;
                    int dx = enemy.Position.x - cell.x;
                    int dz = enemy.Position.z - cell.z;
                    float distanceSquared = dx * dx + dz * dz;
                    if (distanceSquared < minDistanceSquared)
                        return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_PreApplyDamage_GapEscape
    {
        public static bool Prefix(
            Pawn __instance,
            ref DamageInfo dinfo,
            out bool absorbed)
        {
            absorbed = false;
            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == __instance)
                return true;
            if (!GapEmergencyTeleportUtility.IsProtected(__instance))
                return true;
            if (GapEmergencyTeleportUtility.TryEmergencyTeleport(__instance))
            {
                absorbed = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_Kill_GapEscape
    {
        public static bool Prefix(Pawn __instance)
        {
            if (!GapEmergencyTeleportUtility.IsProtected(__instance))
                return true;
            if (GapEmergencyTeleportUtility.TryEmergencyTeleport(__instance))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Destroy")]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_Destroy_GapEscape
    {
        public static bool Prefix(Pawn __instance)
        {
            if (!GapEmergencyTeleportUtility.IsProtected(__instance))
                return true;
            if (GapEmergencyTeleportUtility.TryEmergencyTeleport(__instance))
                return false;
            return true;
        }
    }
}