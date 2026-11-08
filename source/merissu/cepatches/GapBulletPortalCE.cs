using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CombatExtended;
using CombatExtended.Compatibility;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class GapBulletPortalCEStartup
    {
        static GapBulletPortalCEStartup()
        {
            BlockerRegistry.RegisterCheckForCollisionCallback(CheckForCollision);
            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomethingCallback);

            Harmony harmony = new Harmony("merissu.gapbulletportal.ce");

            MethodInfo tickMethod = AccessTools.Method(typeof(ProjectileCE), "Tick");
            if (tickMethod != null)
                harmony.Patch(tickMethod, new HarmonyMethod(typeof(GapBulletPortalCEPatch), "SkipStoredProjectileTick"));

            MethodInfo fireMethod = AccessTools.Method(typeof(Thing_GapBulletPortal), "FireOneProjectile");
            if (fireMethod != null)
                harmony.Patch(fireMethod, new HarmonyMethod(typeof(GapBulletPortalCEPatch), "FireCEProjectile"));
        }

        public static bool CheckForCollision(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            return GapProjectileCEUtility.TryCaptureAtCell(projectile, cell);
        }

        public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
        {
            return GapProjectileCEUtility.TryCaptureAlongPath(projectile);
        }
    }

    public static class GapProjectileCEUtility
    {
        private static readonly Dictionary<Map, List<Thing_GapAura>> gapCache = new Dictionary<Map, List<Thing_GapAura>>();
        private static int gapCacheTick = -1;
        private static readonly Dictionary<int, int> handledProjectiles = new Dictionary<int, int>();

        private static List<Thing_GapAura> GetGaps(Map map)
        {
            if (map == null) return new List<Thing_GapAura>();

            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            if (gapCacheTick != currentTick) { gapCache.Clear(); gapCacheTick = currentTick; }

            List<Thing_GapAura> cached;
            if (gapCache.TryGetValue(map, out cached)) return cached;

            List<Thing_GapAura> result = new List<Thing_GapAura>();
            List<Thing> things = map.listerThings.ThingsOfDef(ThingDef.Named("GapAura"));

            for (int i = 0; i < things.Count; i++)
            {
                Thing_GapAura gap = things[i] as Thing_GapAura;
                if (gap == null || gap.Destroyed || !gap.Spawned) continue;
                result.Add(gap);
            }

            gapCache[map] = result;
            return result;
        }

        public static void MarkHandled(ProjectileCE projectile)
        {
            if (projectile == null) return;

            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            handledProjectiles[projectile.thingIDNumber] = currentTick + 12000;

            if (handledProjectiles.Count > 3000)
            {
                List<int> removeKeys = new List<int>();
                foreach (KeyValuePair<int, int> pair in handledProjectiles)
                    if (pair.Value < currentTick) removeKeys.Add(pair.Key);
                for (int i = 0; i < removeKeys.Count; i++) handledProjectiles.Remove(removeKeys[i]);
            }
        }

        private static bool IsHandled(ProjectileCE projectile)
        {
            if (projectile == null) return false;

            int expiry;
            if (!handledProjectiles.TryGetValue(projectile.thingIDNumber, out expiry)) return false;

            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            if (currentTick <= expiry) return true;

            handledProjectiles.Remove(projectile.thingIDNumber);
            return false;
        }

        public static bool TryCaptureAtCell(ProjectileCE projectile, IntVec3 cell)
        {
            if (projectile == null || projectile.Destroyed || !projectile.Spawned ||
                projectile.Map == null || IsHandled(projectile)) return false;

            Map map = projectile.Map;
            if (!cell.InBounds(map)) return false;

            List<Thing_GapAura> gaps = GetGaps(map);
            for (int i = 0; i < gaps.Count; i++)
            {
                Thing_GapAura gap = gaps[i];
                if (gap == null || gap.Destroyed || !gap.Spawned || gap.controller == null) continue;
                if (!gap.IntersectsProjectileCell(cell)) continue;
                if (CaptureProjectileCopy(projectile, gap)) { MarkHandled(projectile); return true; }
            }
            return false;
        }

        public static bool TryCaptureAlongPath(ProjectileCE projectile)
        {
            if (projectile == null || projectile.Destroyed || !projectile.Spawned ||
                projectile.Map == null || IsHandled(projectile)) return false;

            Vector3 start;
            if (!TryGetProjectileOrigin(projectile, out start)) start = projectile.LastPos;

            return TryCaptureBySegment(projectile, start, projectile.ExactPosition);
        }

        private static bool TryCaptureBySegment(ProjectileCE projectile, Vector3 start, Vector3 end)
        {
            if (projectile == null || projectile.Destroyed || !projectile.Spawned ||
                projectile.Map == null || IsHandled(projectile)) return false;

            List<Thing_GapAura> gaps = GetGaps(projectile.Map);
            for (int i = 0; i < gaps.Count; i++)
            {
                Thing_GapAura gap = gaps[i];
                if (gap == null || gap.Destroyed || !gap.Spawned || gap.controller == null) continue;
                if (!gap.IntersectsProjectilePath(start, end)) continue;
                if (CaptureProjectileCopy(projectile, gap)) { MarkHandled(projectile); return true; }
            }
            return false;
        }

        private static bool TryGetProjectileOrigin(ProjectileCE projectile, out Vector3 origin)
        {
            origin = Vector3.zero;
            FieldInfo field = FindField(projectile.GetType(), "origin");
            if (field != null)
            {
                object value = field.GetValue(projectile);
                if (value is Vector2)
                {
                    Vector2 origin2 = (Vector2)value;
                    origin = new Vector3(origin2.x, projectile.ExactPosition.y, origin2.y);
                    return true;
                }
                if (value is Vector3) { origin = (Vector3)value; return true; }
            }
            return false;
        }

        private static bool CaptureProjectileCopy(ProjectileCE source, Thing_GapAura gap)
        {
            if (source == null || gap == null || gap.controller == null) return false;

            Thing copiedThing = ThingMaker.MakeThing(source.def);
            ProjectileCE copiedProjectile = copiedThing as ProjectileCE;
            if (copiedProjectile == null)
            {
                if (copiedThing != null) copiedThing.Destroy();
                return false;
            }

            CopyProjectileData(source, copiedProjectile);
            return gap.controller.CaptureCEProjectile(copiedProjectile);
        }

        private static void CopyProjectileData(ProjectileCE source, ProjectileCE destination)
        {
            CopyFieldValue(source, destination, "damageDefOverride");
            CopyFieldValue(source, destination, "mass");
            CopyFieldValue(source, destination, "extraDamages");
            CopyPropertyValue(source, destination, "DamageAmount");
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private static void CopyFieldValue(object source, object destination, string name)
        {
            FieldInfo sourceField = FindField(source.GetType(), name);
            FieldInfo destinationField = FindField(destination.GetType(), name);
            if (sourceField == null || destinationField == null) return;

            object sourceValue = sourceField.GetValue(source);
            IList sourceList = sourceValue as IList;
            if (sourceList != null)
            {
                IList destinationList = destinationField.GetValue(destination) as IList;
                if (destinationList == null) return;
                destinationList.Clear();
                for (int i = 0; i < sourceList.Count; i++) destinationList.Add(sourceList[i]);
                return;
            }

            if (destinationField.IsInitOnly) return;
            if (sourceField.FieldType != destinationField.FieldType) return;
            destinationField.SetValue(destination, sourceValue);
        }

        private static void CopyPropertyValue(object source, object destination, string name)
        {
            PropertyInfo sourceProperty = AccessTools.Property(source.GetType(), name);
            PropertyInfo destinationProperty = AccessTools.Property(destination.GetType(), name);
            if (sourceProperty == null || destinationProperty == null ||
                !sourceProperty.CanRead || !destinationProperty.CanWrite) return;
            if (sourceProperty.PropertyType != destinationProperty.PropertyType) return;

            object value = sourceProperty.GetValue(source, null);
            destinationProperty.SetValue(destination, value, null);
        }
    }

    public static class GapBulletPortalCEFireUtility
    {
        private static Pawn FindNearestHostilePawn(Thing_GapBulletPortal portal)
        {
            if (portal == null || portal.Map == null ||
                portal.caster == null || portal.caster.Faction == null) return null;

            Pawn caster = portal.caster;
            IReadOnlyList<Pawn> pawns = portal.Map.mapPawns.AllPawnsSpawned;
            Pawn nearest = null;
            float bestDistanceSquared = float.MaxValue;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (candidate == null || candidate == caster || candidate.Dead ||
                    candidate.Destroyed || !candidate.Spawned || candidate.Faction == null) continue;
                if (!caster.Faction.HostileTo(candidate.Faction)) continue;

                float dx = candidate.Position.x - portal.Position.x;
                float dz = candidate.Position.z - portal.Position.z;
                float distanceSquared = dx * dx + dz * dz;

                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }

            if (nearest != null)
            {
                portal.targetPawn = nearest;
                portal.lastKnownTargetCell = nearest.Position;
            }
            return nearest;
        }

        public static bool TryFire(Thing_GapBulletPortal portal, ProjectileCE projectile)
        {
            if (portal == null || projectile == null || projectile.Destroyed || projectile.Spawned ||
                !portal.Spawned || portal.Map == null || portal.StoredProjectiles.Count <= 0) return false;

            Map map = portal.Map;
            Pawn target = FindNearestHostilePawn(portal);

            IntVec3 aimCell = (target != null && target.Spawned) ? target.Position : portal.lastKnownTargetCell;
            if (!aimCell.IsValid || !aimCell.InBounds(map)) return false;

            ProjectilePropertiesCE properties = projectile.def.projectile as ProjectilePropertiesCE;
            if (properties == null || properties.TrajectoryWorker == null) return false;

            float shotSpeed = properties.speed;
            if (shotSpeed <= 0.001f) return false;

            Vector3 targetPosition = (target != null && target.Spawned)
                ? target.TrueCenter()
                : aimCell.ToVector3Shifted();

            Vector3 sourcePosition = portal.Position.ToVector3Shifted();
            Vector3 flatDirection = targetPosition - sourcePosition;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude < 0.001f) flatDirection = portal.direction;
            if (flatDirection.sqrMagnitude < 0.001f) flatDirection = Vector3.forward;
            flatDirection.Normalize();

            sourcePosition += flatDirection * 0.55f;
            float shotHeight = 1f;
            sourcePosition.y = shotHeight;

            Vector2 origin = new Vector2(sourcePosition.x, sourcePosition.z);
            float distance = Vector2.Distance(origin, new Vector2(targetPosition.x, targetPosition.z));

            float shotAngle = properties.TrajectoryWorker.ShotAngle(properties, sourcePosition, targetPosition, shotSpeed);
            float shotRotation = properties.TrajectoryWorker.ShotRotation(properties, sourcePosition, targetPosition);

            LocalTargetInfo targetInfo = (target != null && target.Spawned)
                ? new LocalTargetInfo(target)
                : new LocalTargetInfo(aimCell);

            Thing resultingThing;
            bool dropped = portal.StoredProjectiles.TryDrop(projectile, portal.Position, map, ThingPlaceMode.Direct, out resultingThing);
            if (!dropped || resultingThing == null) return false;

            ProjectileCE launchedProjectile = resultingThing as ProjectileCE;
            if (launchedProjectile == null) { resultingThing.Destroy(); return true; }

            SetIntendedTarget(launchedProjectile, targetInfo);

            Thing launcher = (portal.caster != null && !portal.caster.Destroyed) ? (Thing)portal.caster : (Thing)portal;

            try
            {
                launchedProjectile.Launch(launcher, origin, shotAngle, shotRotation, shotHeight, shotSpeed, null, distance);
                GapProjectileCEUtility.MarkHandled(launchedProjectile);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[Merissu CE Gap] CE弹丸重新发射失败: " + ex);
                if (!launchedProjectile.Destroyed) launchedProjectile.Destroy();
                return true;
            }
        }

        private static void SetIntendedTarget(ProjectileCE projectile, LocalTargetInfo target)
        {
            FieldInfo field = FindField(projectile.GetType(), "intendedTarget");
            if (field != null)
            {
                if (field.FieldType == typeof(LocalTargetInfo)) field.SetValue(projectile, target);
                else if (typeof(Thing).IsAssignableFrom(field.FieldType)) field.SetValue(projectile, target.Thing);
                return;
            }

            PropertyInfo property = AccessTools.Property(projectile.GetType(), "intendedTarget");
            if (property != null && property.CanWrite && property.PropertyType == typeof(LocalTargetInfo))
                property.SetValue(projectile, target, null);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }
    }

    public static class GapBulletPortalCEPatch
    {
        public static bool SkipStoredProjectileTick(ProjectileCE __instance)
        {
            if (__instance == null || __instance.Destroyed) return false;
            if (!__instance.Spawned || __instance.Map == null) return false;
            return true;
        }

        public static bool FireCEProjectile(Thing_GapBulletPortal __instance, ref bool __result)
        {
            if (__instance == null || __instance.StoredProjectiles.Count <= 0) return true;

            ProjectileCE ceProjectile = __instance.StoredProjectiles[0] as ProjectileCE;
            if (ceProjectile == null) return true;

            __result = GapBulletPortalCEFireUtility.TryFire(__instance, ceProjectile);
            return false;
        }
    }
}