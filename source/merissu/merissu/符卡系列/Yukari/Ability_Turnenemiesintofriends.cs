using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_Turnenemiesintofriends : Ability
    {
        public Ability_Turnenemiesintofriends() : base() { }
        public Ability_Turnenemiesintofriends(Pawn pawn) : base(pawn) { }
        public Ability_Turnenemiesintofriends(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
                if (hp == null || hp.Severity < 5f) return "符卡不足 (需要5张)";
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 5f) return false;
            Map map = pawn.Map;
            if (map == null || !pawn.Spawned) return false;
            if (!base.Activate(target, dest)) return false;
            hp.Severity -= 5f;
            List<Thing_GapAuraController> oldControllers = map.listerThings
                .ThingsOfDef(ThingDef.Named("GapAuraController"))
                .OfType<Thing_GapAuraController>()
                .Where(c => c.caster == pawn)
                .ToList();
            for (int i = 0; i < oldControllers.Count; i++)
            {
                oldControllers[i].DestroyAllGaps();
                oldControllers[i].Destroy();
            }
            Thing_GapAuraController newController = (Thing_GapAuraController)ThingMaker.MakeThing(ThingDef.Named("GapAuraController"));
            newController.Setup(pawn);
            GenSpawn.Spawn(newController, pawn.Position, map);
            return true;
        }
    }

    public class Thing_GapAuraController : Thing, IThingHolder
    {
        public Pawn caster;
        private const int MaxGapCount = 30;
        private const float MinDistance = 2f;
        private const float MaxDistance = 8f;
        private const float PositionJitter = 0.75f;
        private const float MinGapSpacing = 1f;
        private const int ProjectilesPerPortal = 10;
        public bool closing = false;
        public ThingOwner<Thing> storedProjectiles;

        public Thing_GapAuraController()
        {
            storedProjectiles = new ThingOwner<Thing>(this);
        }

        public void Setup(Pawn pawn)
        {
            caster = pawn;
            closing = false;
            if (storedProjectiles == null) storedProjectiles = new ThingOwner<Thing>(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref closing, "closing", false);
            Scribe_Deep.Look(ref storedProjectiles, "storedProjectiles", this);
            if (storedProjectiles == null) storedProjectiles = new ThingOwner<Thing>(this);
        }

        public ThingOwner GetDirectlyHeldThings() { return storedProjectiles; }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        private List<Thing_GapAura> GetOwnedGaps()
        {
            if (Map == null) return new List<Thing_GapAura>();
            return Map.listerThings.ThingsOfDef(ThingDef.Named("GapAura"))
                .OfType<Thing_GapAura>().Where(g => g.controller == this).ToList();
        }

        private List<Thing_GapBulletPortal> GetOwnedBulletPortals()
        {
            if (Map == null) return new List<Thing_GapBulletPortal>();
            return Map.listerThings.ThingsOfDef(ThingDef.Named("GapBulletPortal"))
                .OfType<Thing_GapBulletPortal>().Where(p => p.controller == this).ToList();
        }

        public bool CaptureProjectile(Projectile projectile)
        {
            if (closing || projectile == null || projectile.Destroyed || !projectile.Spawned || Map == null || projectile.Map != Map) return false;
            IntVec3 oldPosition = projectile.Position;
            Map oldMap = projectile.Map;
            projectile.DeSpawn();
            if (!storedProjectiles.TryAdd(projectile))
            {
                if (!projectile.Destroyed && !projectile.Spawned) GenSpawn.Spawn(projectile, oldPosition, oldMap);
                return false;
            }
            TrySpawnQueuedBulletPortals();
            return true;
        }

        public bool CaptureCEProjectile(Thing projectileCopy)
        {
            if (closing || projectileCopy == null || projectileCopy.Destroyed || Map == null || storedProjectiles == null)
            {
                if (projectileCopy != null && !projectileCopy.Destroyed) projectileCopy.Destroy();
                return false;
            }
            if (projectileCopy.Spawned)
            {
                if (projectileCopy.Map != Map) { projectileCopy.Destroy(); return false; }
                projectileCopy.DeSpawn();
            }
            if (!storedProjectiles.TryAdd(projectileCopy))
            {
                if (!projectileCopy.Destroyed) projectileCopy.Destroy();
                return false;
            }
            TrySpawnQueuedBulletPortals();
            return true;
        }

        private Pawn FindNearestHostilePawn(IntVec3 origin)
        {
            if (Map == null || caster == null || caster.Faction == null) return null;
            IReadOnlyList<Pawn> spawnedPawns = Map.mapPawns.AllPawnsSpawned;
            Pawn nearest = null;
            float bestDistanceSquared = float.MaxValue;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn candidate = spawnedPawns[i];
                if (candidate == null || candidate == caster || candidate.Dead || candidate.Destroyed || !candidate.Spawned || candidate.Faction == null) continue;
                if (!caster.Faction.HostileTo(candidate.Faction)) continue;
                float dx = candidate.Position.x - origin.x;
                float dz = candidate.Position.z - origin.z;
                float distanceSquared = dx * dx + dz * dz;
                if (distanceSquared < bestDistanceSquared) { bestDistanceSquared = distanceSquared; nearest = candidate; }
            }
            return nearest;
        }

        private bool TryFindBulletPortalCell(Pawn target, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (target == null || !target.Spawned || Map == null) return false;
            List<IntVec3> candidates = GenRadial.RadialCellsAround(target.Position, 4f, true).ToList();
            List<IntVec3> validCells = new List<IntVec3>();
            for (int i = 0; i < candidates.Count; i++)
            {
                IntVec3 cell = candidates[i];
                if (!cell.InBounds(Map) || !cell.Standable(Map)) continue;
                int dx = cell.x - target.Position.x;
                int dz = cell.z - target.Position.z;
                int distanceSquared = dx * dx + dz * dz;
                if (distanceSquared < 4 || distanceSquared > 16) continue;
                bool occupiedByPawn = false;
                List<Thing> things = cell.GetThingList(Map);
                for (int j = 0; j < things.Count; j++)
                {
                    Pawn otherPawn = things[j] as Pawn;
                    if (otherPawn != null && !otherPawn.Dead && !otherPawn.Destroyed) { occupiedByPawn = true; break; }
                }
                if (occupiedByPawn) continue;
                validCells.Add(cell);
            }
            if (validCells.Count == 0) return false;
            result = validCells[Rand.Range(0, validCells.Count)];
            return true;
        }

        private void TrySpawnQueuedBulletPortals()
        {
            if (closing || Map == null || caster == null || caster.Destroyed || caster.Faction == null || storedProjectiles == null) return;
            while (storedProjectiles.Count >= ProjectilesPerPortal)
            {
                IntVec3 origin = caster.Position;
                Pawn target = FindNearestHostilePawn(origin);
                if (target == null) return;
                IntVec3 portalCell;
                if (!TryFindBulletPortalCell(target, out portalCell)) return;
                Vector3 direction = target.Position.ToVector3Shifted() - portalCell.ToVector3Shifted();
                if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
                direction.Normalize();
                Thing_GapBulletPortal portal = (Thing_GapBulletPortal)ThingMaker.MakeThing(ThingDef.Named("GapBulletPortal"));
                portal.Setup(this, caster, target, direction);
                GenSpawn.Spawn(portal, portalCell, Map);
                if (!portal.Spawned) { portal.Destroy(); return; }
                int transferred = 0;
                while (transferred < ProjectilesPerPortal && storedProjectiles.Count > 0)
                {
                    Thing projectileThing = storedProjectiles[0];
                    int moved = storedProjectiles.TryTransferToContainer(projectileThing, portal.StoredProjectiles, 1, false);
                    if (moved <= 0) break;
                    transferred += moved;
                }
                if (transferred != ProjectilesPerPortal)
                {
                    while (portal.StoredProjectiles.Count > 0)
                    {
                        Thing projectileThing = portal.StoredProjectiles[0];
                        int movedBack = portal.StoredProjectiles.TryTransferToContainer(projectileThing, storedProjectiles, 1, false);
                        if (movedBack <= 0) break;
                    }
                    portal.CancelAndDestroy();
                    return;
                }
                SoundDef.Named("GapBulletPortal").PlayOneShot(new TargetInfo(portalCell, Map));
            }
        }

        protected override void Tick()
        {
            base.Tick();
            List<Thing_GapAura> gaps = GetOwnedGaps();
            if (!closing)
            {
                bool hasHediff = caster != null && !caster.Dead && !caster.Destroyed && caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Turnenemiesintofriends")) != null;
                if (hasHediff && caster.Spawned && caster.Map != Map) hasHediff = false;
                if (!hasHediff) closing = true;
            }
            if (closing)
            {
                for (int i = 0; i < gaps.Count; i++) gaps[i].BeginClose();
                if (storedProjectiles != null && storedProjectiles.Count > 0) storedProjectiles.ClearAndDestroyContents();
                if (gaps.Count == 0 && GetOwnedBulletPortals().Count == 0) Destroy();
                return;
            }
            if (caster == null || !caster.Spawned || caster.Map != Map) return;
            TrySpawnQueuedBulletPortals();
            while (gaps.Count < MaxGapCount) { if (!SpawnGap(gaps)) break; }
        }

        private bool SpawnGap(List<Thing_GapAura> existingGaps)
        {
            if (caster == null || !caster.Spawned || Map == null) return false;
            Vector3 center = caster.Position.ToVector3Shifted();
            float drawX = center.x;
            float drawZ = center.z;
            bool foundPosition = false;
            float minSpacingSquared = MinGapSpacing * MinGapSpacing;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Rand.Range(MinDistance, MaxDistance);
                float candidateX = center.x + Mathf.Cos(angle) * distance + Rand.Range(-PositionJitter, PositionJitter);
                float candidateZ = center.z + Mathf.Sin(angle) * distance + Rand.Range(-PositionJitter, PositionJitter);
                candidateX = Mathf.Clamp(candidateX, 0.5f, Map.Size.x - 0.5f);
                candidateZ = Mathf.Clamp(candidateZ, 0.5f, Map.Size.z - 0.5f);
                drawX = candidateX;
                drawZ = candidateZ;
                bool spacingOK = true;
                for (int i = 0; i < existingGaps.Count; i++)
                {
                    Thing_GapAura other = existingGaps[i];
                    float dx = candidateX - other.drawX;
                    float dz = candidateZ - other.drawZ;
                    if (dx * dx + dz * dz < minSpacingSquared) { spacingOK = false; break; }
                }
                if (spacingOK) { foundPosition = true; break; }
            }
            if (!foundPosition)
            {
                drawX = Mathf.Clamp(drawX, 0.5f, Map.Size.x - 0.5f);
                drawZ = Mathf.Clamp(drawZ, 0.5f, Map.Size.z - 0.5f);
            }
            float scale = Rand.Range(0.9f, 1.5f);
            float rotation = Rand.Range(0f, 360f);
            int appearTicks = Rand.RangeInclusive(13, 18);
            int stayTicks = Rand.RangeInclusive(120, 260);
            int disappearTicks = Rand.RangeInclusive(13, 18);
            Thing_GapAura gap = (Thing_GapAura)ThingMaker.MakeThing(ThingDef.Named("GapAura"));
            gap.Setup(this, drawX, drawZ, rotation, scale, appearTicks, stayTicks, disappearTicks);
            IntVec3 spawnCell = new IntVec3(Mathf.FloorToInt(drawX), 0, Mathf.FloorToInt(drawZ));
            if (!spawnCell.InBounds(Map)) { gap.Destroy(); return false; }
            GenSpawn.Spawn(gap, spawnCell, Map);
            existingGaps.Add(gap);
            return true;
        }

        public void DestroyAllGaps()
        {
            List<Thing_GapAura> gaps = GetOwnedGaps();
            for (int i = 0; i < gaps.Count; i++) gaps[i].Destroy();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            List<Thing_GapBulletPortal> portals = GetOwnedBulletPortals();
            for (int i = 0; i < portals.Count; i++) portals[i].CancelAndDestroy();
            if (storedProjectiles != null && storedProjectiles.Count > 0) storedProjectiles.ClearAndDestroyContents();
            base.Destroy(mode);
        }
    }

    public class Thing_GapAura : Thing
    {
        public Thing_GapAuraController controller;
        public float drawX;
        public float drawZ;
        public float rotation;
        public float visualScale = 1f;
        public float eyeAngle;
        public int age;
        public int appearTicks;
        public int stayTicks;
        public int disappearTicks;
        public bool closing;
        public int closeAge;

        private float closeStartWidth;
        private float closeStartAlpha;

        private const float BaseWidth = 1.6f;
        private const float BaseHeight = 1.6f;
        private const float BaseDepth = 3.2f;
        private const float OverlayOffsetX = 0.05f;
        private const float OverlayOffsetZ = -0.16f;
        private const float OverlayHeightOffset = 0.020f;
        private const float EyeAspectRatio = 1.1f;
        private const float EyeSizeMultiplier = 0.32f;
        private const float EyeOrbitRadiusMultiplier = 0.14f;
        private const float EyeHeightOffset = 0.04f;
        private const float TargetSearchRadius = 30f;
        private const int TargetUpdateInterval = 3;
        private const float PawnContactPadding = 0.35f;
        private const float ProjectileContactPadding = 0.12f;

        private static readonly SoundDef GapKillSound = SoundDef.Named("gapkill");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock overlayPropertyBlock;
        private MaterialPropertyBlock eyePropertyBlock;

        public void Setup(
            Thing_GapAuraController controller,
            float drawX,
            float drawZ,
            float rotation,
            float visualScale,
            int appearTicks,
            int stayTicks,
            int disappearTicks)
        {
            this.controller = controller;
            this.drawX = drawX;
            this.drawZ = drawZ;
            this.rotation = rotation;
            this.visualScale = visualScale;
            this.appearTicks = appearTicks;
            this.stayTicks = stayTicks;
            this.disappearTicks = disappearTicks;
            age = 0;
            closing = false;
            closeAge = 0;
            closeStartWidth = 0f;
            closeStartAlpha = 0f;
            eyeAngle = rotation;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref controller, "controller");
            Scribe_Values.Look(ref drawX, "drawX");
            Scribe_Values.Look(ref drawZ, "drawZ");
            Scribe_Values.Look(ref rotation, "rotation");
            Scribe_Values.Look(ref visualScale, "visualScale", 1f);
            Scribe_Values.Look(ref eyeAngle, "eyeAngle", 0f);
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref appearTicks, "appearTicks", 15);
            Scribe_Values.Look(ref stayTicks, "stayTicks", 180);
            Scribe_Values.Look(ref disappearTicks, "disappearTicks", 15);
            Scribe_Values.Look(ref closing, "closing", false);
            Scribe_Values.Look(ref closeAge, "closeAge", 0);
            Scribe_Values.Look(ref closeStartWidth, "closeStartWidth", 0f);
            Scribe_Values.Look(ref closeStartAlpha, "closeStartAlpha", 0f);
        }

        private float GetOpeningProgress() => Mathf.Clamp01(age / (float)Mathf.Max(1, appearTicks));

        private float GetCurrentWidth() => BaseWidth * visualScale * GetOpeningProgress();

        private float GetGapCurrentWidth()
        {
            if (closing)
            {
                float progress = Mathf.Clamp01(closeAge / (float)Mathf.Max(1, disappearTicks));
                return Mathf.Lerp(closeStartWidth, 0f, progress);
            }
            return GetCurrentWidth();
        }

        private float GetOverlayAlpha()
        {
            if (closing)
            {
                float progress = Mathf.Clamp01(closeAge / (float)Mathf.Max(1, disappearTicks));
                return closeStartAlpha * (1f - progress);
            }
            return GetOpeningProgress();
        }

        private float GetOverlayScale()
        {
            if (!closing) return 1f;
            float progress = Mathf.Clamp01(closeAge / (float)Mathf.Max(1, disappearTicks));
            return 1f - progress;
        }

        private Vector3 GetOverlayCenter()
        {
            Quaternion gapRotation = Quaternion.Euler(0f, rotation, 0f);
            Vector3 localOffset = gapRotation * new Vector3(OverlayOffsetX, 0f, OverlayOffsetZ);
            return new Vector3(drawX + localOffset.x, 0f, drawZ + localOffset.z);
        }

        private void UpdateEyeTracking()
        {
            if (closing || Map == null || controller == null) return;
            if (age % TargetUpdateInterval != 0) return;
            Pawn caster = controller.caster;
            if (caster == null || caster.Destroyed || caster.Faction == null) return;
            Vector3 center = GetOverlayCenter();
            Pawn nearestEnemy = null;
            float bestDistanceSquared = TargetSearchRadius * TargetSearchRadius;
            IReadOnlyList<Pawn> spawnedPawns = Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn candidate = spawnedPawns[i];
                if (candidate == null || candidate == caster || candidate.Dead || candidate.Destroyed || !candidate.Spawned || candidate.Faction == null) continue;
                if (!caster.Faction.HostileTo(candidate.Faction)) continue;
                Vector3 enemyPos = candidate.Position.ToVector3Shifted();
                float dx = enemyPos.x - center.x;
                float dz = enemyPos.z - center.z;
                float distanceSquared = dx * dx + dz * dz;
                if (distanceSquared > bestDistanceSquared) continue;
                bestDistanceSquared = distanceSquared;
                nearestEnemy = candidate;
            }
            if (nearestEnemy != null)
            {
                Vector3 enemyPos = nearestEnemy.Position.ToVector3Shifted();
                float dx = enemyPos.x - center.x;
                float dz = enemyPos.z - center.z;
                eyeAngle = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
            }
        }

        private bool TryConsumeEnemy()
        {
            if (closing || !Spawned || Map == null || controller == null) return false;
            Pawn caster = controller.caster;
            if (caster == null || caster.Destroyed || caster.Faction == null) return false;
            float currentWidth = GetGapCurrentWidth();
            if (currentWidth <= 0.01f) return false;
            Quaternion gapRotation = Quaternion.Euler(0f, rotation, 0f);
            Quaternion inverseRotation = Quaternion.Inverse(gapRotation);
            float halfWidth = currentWidth * 0.5f + PawnContactPadding;
            float halfDepth = BaseDepth * visualScale * 0.5f + PawnContactPadding;
            Vector3 gapCenter = new Vector3(drawX, 0f, drawZ);
            IReadOnlyList<Pawn> spawnedPawns = Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn targetPawn = spawnedPawns[i];
                if (targetPawn == null || targetPawn == caster || targetPawn.Dead || targetPawn.Destroyed || !targetPawn.Spawned || targetPawn.Faction == null) continue;
                if (!caster.Faction.HostileTo(targetPawn.Faction)) continue;
                Vector3 pawnPos = targetPawn.Position.ToVector3Shifted();
                Vector3 worldDelta = new Vector3(pawnPos.x - gapCenter.x, 0f, pawnPos.z - gapCenter.z);
                Vector3 localDelta = inverseRotation * worldDelta;
                if (Mathf.Abs(localDelta.x) > halfWidth) continue;
                if (Mathf.Abs(localDelta.z) > halfDepth) continue;
                ExecuteGapKill(targetPawn);
                return true;
            }
            return false;
        }

        public bool IntersectsProjectileCell(IntVec3 cell)
        {
            if (!Spawned || Map == null || controller == null || controller.closing || !cell.InBounds(Map)) return false;

            float alpha = GetOverlayAlpha();
            if (alpha <= 0.01f) return false;

            float gapWidth = Mathf.Max(GetGapCurrentWidth(), BaseWidth * visualScale * GetOverlayScale());
            if (gapWidth <= 0.01f) return false;

            float radians = rotation * Mathf.Deg2Rad;
            float cellHalfExtent = 0.5f * (Mathf.Abs(Mathf.Cos(radians)) + Mathf.Abs(Mathf.Sin(radians)));

            float halfWidth = gapWidth * 0.5f + ProjectileContactPadding + cellHalfExtent;
            float halfDepth = BaseDepth * visualScale * 0.5f + ProjectileContactPadding + cellHalfExtent;

            Quaternion gapRotation = Quaternion.Euler(0f, rotation, 0f);
            Quaternion inverseRotation = Quaternion.Inverse(gapRotation);

            Vector3 cellCenter = cell.ToVector3Shifted();
            Vector3 worldDelta = new Vector3(cellCenter.x - drawX, 0f, cellCenter.z - drawZ);
            Vector3 localDelta = inverseRotation * worldDelta;

            if (Mathf.Abs(localDelta.x) > halfWidth) return false;
            if (Mathf.Abs(localDelta.z) > halfDepth) return false;
            return true;
        }

        public bool IntersectsProjectilePath(Vector3 start, Vector3 end)
        {
            if (!Spawned || Map == null || controller == null || controller.closing) return false;
            float alpha = GetOverlayAlpha();
            if (alpha <= 0.01f) return false;
            float gapWidth = Mathf.Max(GetGapCurrentWidth(), BaseWidth * visualScale * GetOverlayScale());
            if (gapWidth <= 0.01f) return false;
            float halfWidth = gapWidth * 0.5f + ProjectileContactPadding;
            float halfDepth = BaseDepth * visualScale * 0.5f + ProjectileContactPadding;
            Quaternion gapRotation = Quaternion.Euler(0f, rotation, 0f);
            Quaternion inverseRotation = Quaternion.Inverse(gapRotation);
            Vector3 localStart = inverseRotation * new Vector3(start.x - drawX, 0f, start.z - drawZ);
            Vector3 localEnd = inverseRotation * new Vector3(end.x - drawX, 0f, end.z - drawZ);
            return SegmentIntersectsRectangle(localStart.x, localStart.z, localEnd.x, localEnd.z, halfWidth, halfDepth);
        }

        private bool SegmentIntersectsRectangle(float startX, float startZ, float endX, float endZ, float halfWidth, float halfDepth)
        {
            float deltaX = endX - startX;
            float deltaZ = endZ - startZ;
            float tMin = 0f;
            float tMax = 1f;
            if (!ClipProjectileAxis(startX, deltaX, -halfWidth, halfWidth, ref tMin, ref tMax)) return false;
            if (!ClipProjectileAxis(startZ, deltaZ, -halfDepth, halfDepth, ref tMin, ref tMax)) return false;
            return tMin <= tMax;
        }

        private bool ClipProjectileAxis(float start, float delta, float min, float max, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(delta) < 0.000001f) return start >= min && start <= max;
            float t1 = (min - start) / delta;
            float t2 = (max - start) / delta;
            if (t1 > t2) { float temp = t1; t1 = t2; t2 = temp; }
            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;
            return tMin <= tMax;
        }

        private void ExecuteGapKill(Pawn targetPawn)
        {
            if (targetPawn == null || targetPawn.Destroyed || !targetPawn.Spawned) return;
            Map map = Map;
            if (map == null) return;
            IntVec3 targetCell = targetPawn.Position;
            targetPawn.TryGetComp<CompCanBeDormant>()?.WakeUp();
            GapKillSound?.PlayOneShot(new TargetInfo(targetCell, map));
            FleckMaker.ThrowDustPuff(targetCell, map, 2f);
            BeginClose();
            Thing_GapKiller killer = (Thing_GapKiller)ThingMaker.MakeThing(ThingDef.Named("GapKiller"));
            killer.isPawn = true;
            GenSpawn.Spawn(killer, targetCell, map);
            targetPawn.DeSpawn();
            killer.innerContainer.TryAdd(targetPawn);
            killer.CacheTexture();
        }

        public void BeginClose()
        {
            if (closing) return;
            closeStartWidth = GetCurrentWidth();
            closeStartAlpha = GetOpeningProgress();
            closing = true;
            closeAge = 0;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (closing)
            {
                closeAge++;
                if (closeAge >= Mathf.Max(1, disappearTicks)) Destroy();
                return;
            }
            UpdateEyeTracking();
            if (TryConsumeEnemy()) return;
            if (age >= appearTicks + stayTicks) BeginClose();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!Spawned) return;
            if (controller == null || controller.Destroyed) return;
            Vector3 basePos = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);
            Vector3 gapPos = basePos;
            gapPos.x = drawX;
            gapPos.z = drawZ;
            gapPos.y += 0.9f;
            Quaternion gapRotation = Quaternion.Euler(0f, rotation, 0f);
            int gapFrame = age % 13;
            float gapCurrentWidth = GetGapCurrentWidth();
            Material gapMat = MaterialPool.MatFrom("Other/gap/gapkill/bulletEa" + gapFrame.ToString("D3"), ShaderDatabase.Mote);
            Matrix4x4 gapMatrix = Matrix4x4.TRS(gapPos, gapRotation, new Vector3(gapCurrentWidth, BaseHeight * visualScale, BaseDepth * visualScale));
            Graphics.DrawMesh(MeshPool.plane10, gapMatrix, gapMat, 0);
            int overlayFrame = age % 15;
            Material overlayMat = MaterialPool.MatFrom("Other/gap/Turnenemiesintofriends/bulletJa" + overlayFrame.ToString("D3"), ShaderDatabase.MoteGlow);
            float overlayAlpha = Mathf.Clamp01(GetOverlayAlpha());
            float overlayScale = GetOverlayScale();
            Vector3 overlayPos = gapPos;
            Vector3 localOffset = gapRotation * new Vector3(OverlayOffsetX, 0f, OverlayOffsetZ);
            overlayPos += localOffset;
            overlayPos.y += OverlayHeightOffset;
            float overlaySize = BaseWidth * visualScale * overlayScale;
            Matrix4x4 overlayMatrix = Matrix4x4.TRS(overlayPos, gapRotation, new Vector3(overlaySize, 1f, overlaySize));
            if (overlayPropertyBlock == null) overlayPropertyBlock = new MaterialPropertyBlock();
            overlayPropertyBlock.Clear();
            overlayPropertyBlock.SetColor(ColorProperty, new Color(1f, 1f, 1f, overlayAlpha));
            Graphics.DrawMesh(MeshPool.plane10, overlayMatrix, overlayMat, 0, null, 0, overlayPropertyBlock);
            Material eyeMat = MaterialPool.MatFrom("Other/gap/Turnenemiesintofriends/bulletJb000", ShaderDatabase.Mote);
            float eyeOrbitRadius = overlaySize * EyeOrbitRadiusMultiplier;
            float eyeRadians = eyeAngle * Mathf.Deg2Rad;
            Vector3 eyePos = overlayPos;
            eyePos.x += Mathf.Cos(eyeRadians) * eyeOrbitRadius;
            eyePos.z += Mathf.Sin(eyeRadians) * eyeOrbitRadius;
            eyePos.y += EyeHeightOffset;
            Quaternion eyeRotation = Quaternion.Euler(0f, -eyeAngle, 0f);
            float eyeWidth = overlaySize * EyeAspectRatio * EyeSizeMultiplier;
            float eyeDepth = overlaySize * EyeSizeMultiplier;
            Matrix4x4 eyeMatrix = Matrix4x4.TRS(eyePos, eyeRotation, new Vector3(eyeWidth, 1f, eyeDepth));
            if (eyePropertyBlock == null) eyePropertyBlock = new MaterialPropertyBlock();
            eyePropertyBlock.Clear();
            eyePropertyBlock.SetColor(ColorProperty, new Color(1f, 1f, 1f, overlayAlpha));
            Graphics.DrawMesh(MeshPool.plane10, eyeMatrix, eyeMat, 0, null, 0, eyePropertyBlock);
        }
    }

    public class Thing_GapBulletPortal : Thing, IThingHolder
    {
        public Thing_GapAuraController controller;
        public Pawn caster;
        public Pawn targetPawn;
        public IntVec3 lastKnownTargetCell = IntVec3.Invalid;
        public Vector3 direction = Vector3.forward;

        private ThingOwner<Thing> storedProjectiles;
        private int age;
        private int nextShotTick = -1;
        private bool isClosing;
        private int closeStartTick = -1;
        private Sustainer sustainer;

        private const int ExpandDuration = 15;
        private const int ShrinkDuration = 10;
        private const int TicksPerFrame = 4;
        private const int TotalFrames = 16;
        private const int TicksBetweenShots = 4;
        private const float FinalWidth = 1f;
        private const float Height = 2f;
        private static Mesh portalMesh;

        private static readonly FieldInfo WeaponDamageMultiplierField = AccessTools.Field(typeof(Projectile), "weaponDamageMultiplier");
        private static readonly FieldInfo EquipmentDefField = AccessTools.Field(typeof(Projectile), "equipmentDef");
        private static readonly FieldInfo TargetCoverDefField = AccessTools.Field(typeof(Projectile), "targetCoverDef");

        public Thing_GapBulletPortal()
        {
            storedProjectiles = new ThingOwner<Thing>(this);
        }

        public ThingOwner<Thing> StoredProjectiles
        {
            get
            {
                if (storedProjectiles == null) storedProjectiles = new ThingOwner<Thing>(this);
                return storedProjectiles;
            }
        }

        public void Setup(Thing_GapAuraController controller, Pawn caster, Pawn targetPawn, Vector3 direction)
        {
            this.controller = controller;
            this.caster = caster;
            this.targetPawn = targetPawn;
            this.lastKnownTargetCell = targetPawn != null ? targetPawn.Position : IntVec3.Invalid;
            this.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            age = 0;
            nextShotTick = ExpandDuration;
            isClosing = false;
            closeStartTick = -1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref controller, "controller");
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref targetPawn, "targetPawn");
            Scribe_Values.Look(ref lastKnownTargetCell, "lastKnownTargetCell", IntVec3.Invalid);
            Scribe_Values.Look(ref direction, "direction", Vector3.forward);
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref nextShotTick, "nextShotTick", -1);
            Scribe_Values.Look(ref isClosing, "isClosing", false);
            Scribe_Values.Look(ref closeStartTick, "closeStartTick", -1);
            Scribe_Deep.Look(ref storedProjectiles, "storedProjectiles", this);
            if (storedProjectiles == null) storedProjectiles = new ThingOwner<Thing>(this);
        }

        public ThingOwner GetDirectlyHeldThings() { return StoredProjectiles; }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        private static Mesh PortalMesh
        {
            get
            {
                if (portalMesh == null)
                {
                    portalMesh = new Mesh();
                    portalMesh.name = "GapBulletPortal_Mesh";
                    portalMesh.vertices = new Vector3[]
                    {
                        new Vector3(-0.5f, 0f, -0.5f),
                        new Vector3(0.5f, 0f, -0.5f),
                        new Vector3(-0.5f, 0f, 0.5f),
                        new Vector3(0.5f, 0f, 0.5f)
                    };
                    portalMesh.uv = new Vector2[]
                    {
                        new Vector2(0.01f, 0.01f),
                        new Vector2(0.99f, 0.01f),
                        new Vector2(0.01f, 0.99f),
                        new Vector2(0.99f, 0.99f)
                    };
                    portalMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    portalMesh.RecalculateNormals();
                }
                return portalMesh;
            }
        }

        private Pawn FindNearestHostilePawn()
        {
            if (Map == null || caster == null || caster.Faction == null) return null;
            IReadOnlyList<Pawn> spawnedPawns = Map.mapPawns.AllPawnsSpawned;
            Pawn nearest = null;
            float bestDistanceSquared = float.MaxValue;
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn candidate = spawnedPawns[i];
                if (candidate == null || candidate == caster || candidate.Dead || candidate.Destroyed || !candidate.Spawned || candidate.Faction == null) continue;
                if (!caster.Faction.HostileTo(candidate.Faction)) continue;
                float dx = candidate.Position.x - Position.x;
                float dz = candidate.Position.z - Position.z;
                float distanceSquared = dx * dx + dz * dz;
                if (distanceSquared < bestDistanceSquared) { bestDistanceSquared = distanceSquared; nearest = candidate; }
            }
            if (nearest != null)
            {
                targetPawn = nearest;
                lastKnownTargetCell = nearest.Position;
            }
            if (targetPawn != null && targetPawn.Spawned && !targetPawn.Dead && !targetPawn.Destroyed && targetPawn.Faction != null && caster.Faction.HostileTo(targetPawn.Faction))
            {
                lastKnownTargetCell = targetPawn.Position;
                return targetPawn;
            }
            return nearest;
        }

        private bool FireOneProjectile()
        {
            if (Map == null || StoredProjectiles.Count <= 0) return false;
            Pawn target = FindNearestHostilePawn();
            if (target != null && target.Spawned) lastKnownTargetCell = target.Position;
            if (target == null && !lastKnownTargetCell.InBounds(Map)) return false;
            Projectile projectile = StoredProjectiles[0] as Projectile;
            if (projectile == null)
            {
                Thing invalidThing = StoredProjectiles[0];
                StoredProjectiles.Remove(invalidThing);
                invalidThing.Destroy();
                return true;
            }
            float originalDamageMultiplier = 1f;
            ThingDef originalEquipmentDef = null;
            ThingDef originalTargetCoverDef = null;
            if (WeaponDamageMultiplierField != null)
            {
                object value = WeaponDamageMultiplierField.GetValue(projectile);
                if (value is float) originalDamageMultiplier = (float)value;
            }
            if (EquipmentDefField != null) originalEquipmentDef = EquipmentDefField.GetValue(projectile) as ThingDef;
            if (TargetCoverDefField != null) originalTargetCoverDef = TargetCoverDefField.GetValue(projectile) as ThingDef;
            Thing resultingThing;
            bool dropped = StoredProjectiles.TryDrop(projectile, Position, Map, ThingPlaceMode.Direct, out resultingThing);
            if (!dropped || resultingThing == null) return false;
            Projectile launchedProjectile = resultingThing as Projectile;
            if (launchedProjectile == null) { resultingThing.Destroy(); return true; }
            IntVec3 aimCell = target != null && target.Spawned ? target.Position : lastKnownTargetCell;
            Vector3 launchDirection = aimCell.ToVector3Shifted() - Position.ToVector3Shifted();
            if (launchDirection.sqrMagnitude < 0.001f) launchDirection = direction;
            launchDirection.Normalize();
            Vector3 launchOrigin = Position.ToVector3Shifted() + launchDirection * 1.1f;
            LocalTargetInfo targetInfo = target != null && target.Spawned ? new LocalTargetInfo(target) : new LocalTargetInfo(aimCell);
            Thing launcher = caster != null && !caster.Destroyed ? (Thing)caster : (Thing)this;
            launchedProjectile.Launch(launcher, launchOrigin, targetInfo, targetInfo, ProjectileHitFlags.All, false, null, originalTargetCoverDef);
            if (WeaponDamageMultiplierField != null) WeaponDamageMultiplierField.SetValue(launchedProjectile, originalDamageMultiplier);
            if (EquipmentDefField != null) EquipmentDefField.SetValue(launchedProjectile, originalEquipmentDef);
            if (TargetCoverDefField != null) TargetCoverDefField.SetValue(launchedProjectile, originalTargetCoverDef);
            GapProjectileCaptureUtility.MarkReflected(launchedProjectile);
            return true;
        }

        private void MaintainMovingSound()
        {
            if (sustainer == null || sustainer.Ended)
            {
                SoundDef movingSound = SoundDef.Named("TrainMoving");
                if (movingSound != null) sustainer = movingSound.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
            }
            else sustainer.Maintain();
        }

        private void StopMovingSound()
        {
            if (sustainer != null && !sustainer.Ended) sustainer.End();
            sustainer = null;
        }

        public void BeginClose()
        {
            if (isClosing) return;
            isClosing = true;
            closeStartTick = age;
            StopMovingSound();
        }

        public void CancelAndDestroy()
        {
            StopMovingSound();
            if (StoredProjectiles.Count > 0) StoredProjectiles.ClearAndDestroyContents();
            if (Spawned) Destroy();
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            age++;
            if (!isClosing && StoredProjectiles.Count <= 0) BeginClose();
            if (!isClosing && age >= ExpandDuration && age >= nextShotTick)
            {
                if (FireOneProjectile()) nextShotTick = age + TicksBetweenShots;
                else nextShotTick = age + 10;
                if (StoredProjectiles.Count <= 0) BeginClose();
            }
            if (!isClosing && age >= ExpandDuration && StoredProjectiles.Count > 0) MaintainMovingSound();
            else StopMovingSound();
            if (isClosing && age >= closeStartTick + ShrinkDuration) Destroy();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            StopMovingSound();
            if (storedProjectiles != null && storedProjectiles.Count > 0) storedProjectiles.ClearAndDestroyContents();
            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!Spawned) return;
            float width;
            if (isClosing)
            {
                float progress = Mathf.Clamp01((float)(age - closeStartTick) / ShrinkDuration);
                width = Mathf.Lerp(FinalWidth, 0f, progress);
            }
            else if (age < ExpandDuration) width = Mathf.Lerp(0f, FinalWidth, (float)age / ExpandDuration);
            else width = FinalWidth;
            if (width <= 0.001f) return;
            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.1f;
            Quaternion portalRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, -90f, 0f);
            int frame = (age % (TicksPerFrame * TotalFrames)) / TicksPerFrame;
            Material mat = MaterialPool.MatFrom("Other/gap/bulletEa" + frame.ToString("D3"), ShaderDatabase.MoteGlow);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, portalRotation, new Vector3(width, 1f, Height));
            Graphics.DrawMesh(PortalMesh, matrix, mat, 0);
        }
    }

    public static class GapProjectileCaptureUtility
    {
        private static readonly Dictionary<int, int> reflectedProjectiles = new Dictionary<int, int>();
        private static readonly FieldInfo LauncherField = AccessTools.Field(typeof(Projectile), "launcher");

        public static void MarkReflected(Projectile projectile)
        {
            if (projectile == null) return;
            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            reflectedProjectiles[projectile.thingIDNumber] = currentTick + 12000;
        }

        private static bool IsReflected(Projectile projectile)
        {
            if (projectile == null) return false;
            int expiry;
            if (!reflectedProjectiles.TryGetValue(projectile.thingIDNumber, out expiry)) return false;
            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            if (currentTick <= expiry) return true;
            reflectedProjectiles.Remove(projectile.thingIDNumber);
            return false;
        }

        public static bool TryCaptureProjectile(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
        {
            if (projectile == null || projectile.Destroyed || !projectile.Spawned || projectile.Map == null || IsReflected(projectile)) return false;
            List<Thing> gaps = projectile.Map.listerThings.ThingsOfDef(ThingDef.Named("GapAura"));
            for (int i = 0; i < gaps.Count; i++)
            {
                Thing_GapAura gap = gaps[i] as Thing_GapAura;
                if (gap == null || gap.Destroyed || !gap.Spawned || gap.controller == null) continue;
                if (!gap.IntersectsProjectilePath(lastExactPos, newExactPos)) continue;
                if (gap.controller.CaptureProjectile(projectile)) return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Projectile), "TickInterval")]
    public static class Patch_Projectile_TickInterval_GapStored
    {
        public static bool Prefix(Projectile __instance)
        {
            if (__instance == null) return false;
            if (__instance.Destroyed) return false;
            if (!__instance.Spawned || __instance.Map == null) return false;
            return true;
        }
    }

    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Projectile_CheckForFreeInterceptBetween_GapCapture
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Projectile), "CheckForFreeInterceptBetween", new Type[] { typeof(Vector3), typeof(Vector3) });
        }

        public static bool Prefix(Projectile __instance, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result)
        {
            if (GapProjectileCaptureUtility.TryCaptureProjectile(__instance, lastExactPos, newExactPos))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}