using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class HediffCompProperties_GhostButterflyVisuals
        : HediffCompProperties
    {
        public int butterflyCount = 12;
        public float speedMultiplier = 1f;
        public string requiredHediffDefName = "Hediff_GhostButterfly";

        public HediffCompProperties_GhostButterflyVisuals()
        {
            compClass = typeof(HediffComp_GhostButterflyVisuals);
        }
    }

    public class HediffComp_GhostButterflyVisuals : HediffComp
    {
        private Thing_GhostButterflyOrbitController controller;

        public override void CompPostMake()
        {
            base.CompPostMake();
            TrySpawnController();
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            TrySpawnController();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent.pawn;

            if (pawn == null)
            {
                RemoveController();
                return;
            }

            if (!pawn.Spawned || pawn.Map == null)
            {
                RemoveController();
                return;
            }

            TrySpawnController();
        }

        private void TrySpawnController()
        {
            Pawn pawn = parent.pawn;

            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return;

            if (controller != null
                && !controller.Destroyed
                && controller.Spawned
                && controller.Map == pawn.Map)
            {
                return;
            }

            if (controller != null && !controller.Destroyed)
                controller.Destroy();

            controller = null;

            ThingDef controllerDef =
                ThingDef.Named("GhostButterflyOrbitController");

            controller =
                (Thing_GhostButterflyOrbitController)
                ThingMaker.MakeThing(controllerDef);

            controller.pawn = pawn;

            HediffCompProperties_GhostButterflyVisuals settings =
                (HediffCompProperties_GhostButterflyVisuals)props;

            controller.butterflyCount = Mathf.Max(1, settings.butterflyCount);
            controller.speedMultiplier = Mathf.Clamp(
                settings.speedMultiplier,
                0.01f,
                5f);

            controller.requiredHediffDefName =
                string.IsNullOrEmpty(settings.requiredHediffDefName)
                    ? "Hediff_GhostButterfly"
                    : settings.requiredHediffDefName;

            GenSpawn.Spawn(
                controller,
                pawn.Position,
                pawn.Map);
        }

        private void RemoveController()
        {
            if (controller != null && !controller.Destroyed)
                controller.Destroy();

            controller = null;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            Scribe_References.Look(
                ref controller,
                "ghostButterflyController");
        }
    }

    public class Thing_GhostButterflyOrbitController : Thing
    {
        public Pawn pawn;

        public int butterflyCount = 12;
        public float speedMultiplier = 1f;
        public string requiredHediffDefName = "Hediff_GhostButterfly";

        private List<Mote_GhostButterflyOrbit> butterflies =
            new List<Mote_GhostButterflyOrbit>();

        private bool rebuildingAfterLoad;
        private int rebuildWait;

        public override void SpawnSetup(
            Map map,
            bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (butterflies == null)
                butterflies = new List<Mote_GhostButterflyOrbit>();

            if (respawningAfterLoad)
            {
                rebuildingAfterLoad = true;
                rebuildWait = 15;
            }
            else
            {
                rebuildingAfterLoad = false;
                EnsureButterflies();
            }
        }

        protected override void Tick()
        {
            Pawn currentPawn = pawn;

            if (currentPawn == null
                || currentPawn.Dead
                || !currentPawn.Spawned
                || currentPawn.Map == null
                || currentPawn.Map != Map)
            {
                Destroy();
                return;
            }

            if (currentPawn.health == null
                || string.IsNullOrEmpty(requiredHediffDefName)
                || !currentPawn.health.hediffSet.HasHediff(
                    HediffDef.Named(requiredHediffDefName)))
            {
                Destroy();
                return;
            }

            if (rebuildingAfterLoad)
            {
                if (rebuildWait > 0)
                {
                    rebuildWait--;
                    return;
                }

                RebuildButterflies();
                rebuildingAfterLoad = false;
            }

            EnsureButterflies();
        }

        public Vector3 GetCenter()
        {
            if (pawn == null)
                return Vector3.zero;

            Vector3 center = pawn.DrawPos;
            center.y = 0f;

            return center;
        }

        private void RebuildButterflies()
        {
            if (butterflies == null)
            {
                butterflies =
                    new List<Mote_GhostButterflyOrbit>();
            }

            butterflies.Clear();

            if (Map == null)
                return;

            List<Thing> spawnedThings =
                Map.listerThings.ThingsOfDef(
                    ThingDef.Named("Mote_GhostButterflyOrbit"));

            for (int i = 0; i < spawnedThings.Count; i++)
            {
                Mote_GhostButterflyOrbit mote =
                    spawnedThings[i] as Mote_GhostButterflyOrbit;

                if (mote == null || mote.Destroyed)
                    continue;

                if (mote.controller == this)
                {
                    mote.controller = this;
                    butterflies.Add(mote);
                }
            }
        }

        private void EnsureButterflies()
        {
            if (butterflies == null)
            {
                butterflies =
                    new List<Mote_GhostButterflyOrbit>();
            }

            for (int i = butterflies.Count - 1; i >= 0; i--)
            {
                Mote_GhostButterflyOrbit mote = butterflies[i];

                if (mote == null
                    || mote.Destroyed
                    || !mote.Spawned
                    || mote.Map != Map)
                {
                    butterflies.RemoveAt(i);
                    continue;
                }

                mote.controller = this;
            }

            butterflyCount = Mathf.Max(1, butterflyCount);

            while (butterflies.Count > butterflyCount)
            {
                int lastIndex = butterflies.Count - 1;

                Mote_GhostButterflyOrbit mote =
                    butterflies[lastIndex];

                butterflies.RemoveAt(lastIndex);

                if (mote != null && !mote.Destroyed)
                {
                    mote.controller = null;
                    mote.Destroy();
                }
            }

            while (butterflies.Count < butterflyCount)
            {
                SpawnButterfly(true);
            }
        }

        private void SpawnButterfly(bool staggered)
        {
            if (pawn == null || !pawn.Spawned || Map == null)
                return;

            Mote_GhostButterflyOrbit mote =
                (Mote_GhostButterflyOrbit)
                ThingMaker.MakeThing(
                    ThingDef.Named("Mote_GhostButterflyOrbit"));

            mote.controller = this;
            mote.Initialize(GetCenter(), staggered);

            GenSpawn.Spawn(
                mote,
                pawn.Position,
                Map);

            butterflies.Add(mote);
        }

        protected override void DrawAt(
            Vector3 drawLoc,
            bool flip = false)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref pawn, "pawn");

            Scribe_Values.Look(ref butterflyCount, "butterflyCount", 12);
            Scribe_Values.Look(ref speedMultiplier, "speedMultiplier", 1f);
            Scribe_Values.Look(
                ref requiredHediffDefName,
                "requiredHediffDefName",
                "Hediff_GhostButterfly");

            Scribe_Collections.Look(
                ref butterflies,
                "butterflies",
                LookMode.Reference);

            Scribe_Values.Look(
                ref rebuildingAfterLoad,
                "rebuildingAfterLoad",
                false);

            Scribe_Values.Look(
                ref rebuildWait,
                "rebuildWait",
                0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit
                && butterflies == null)
            {
                butterflies =
                    new List<Mote_GhostButterflyOrbit>();
            }
        }

        public override void Destroy(
            DestroyMode mode = DestroyMode.Vanish)
        {
            if (butterflies != null)
            {
                for (int i = butterflies.Count - 1; i >= 0; i--)
                {
                    Mote_GhostButterflyOrbit mote = butterflies[i];

                    if (mote == null || mote.Destroyed)
                        continue;

                    mote.controller = null;
                    mote.Destroy(mode);
                }

                butterflies.Clear();
            }

            base.Destroy(mode);
        }
    }

    [StaticConstructorOnStartup]
    public class Mote_GhostButterflyOrbit : Thing
    {
        public Thing_GhostButterflyOrbitController controller;

        private const int FrameCount = 14;
        private const int FrameInterval = 3;

        private const int MinLifeTicks = 180;
        private const int MaxLifeTicks = 361;

        private const float MinSpawnRadius = 0.35f;
        private const float MaxSpawnRadius = 1.5f;
        private const float MaxOrbitRadius = 1.5f;

        private const float GravityStrength = 0.0032f;

        private int age;
        private int lifeTicks;

        private int fadeInTicks;
        private int fadeOutTicks;

        private int frame;
        private int frameTick;

        private int orbitDirection;

        private float size;

        private Vector3 pos;
        private Vector3 velocity;
        private Vector3 lastCenter;

        private bool initialized;

        private static readonly MaterialPropertyBlock propertyBlock =
            new MaterialPropertyBlock();

        public void Initialize(Vector3 center, bool staggered)
        {
            center.y = 0f;

            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;

            float radius = Rand.Range(
                MinSpawnRadius,
                MaxSpawnRadius);

            Vector3 radial = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));

            pos = center + radial * radius;
            lastCenter = center;

            orbitDirection = Rand.Range(0, 2) == 0 ? -1 : 1;

            float speedMultiplier = controller != null
                ? Mathf.Clamp(controller.speedMultiplier, 0.01f, 5f)
                : 1f;

            float orbitSpeed = Rand.Range(0.028f, 0.058f)
                * speedMultiplier;

            Vector3 tangent = new Vector3(
                -radial.z,
                0f,
                radial.x);

            tangent *= orbitDirection;

            float radialSpeed = Rand.Range(-0.008f, 0.008f)
                * speedMultiplier;

            velocity =
                tangent * orbitSpeed
                + radial * radialSpeed;

            size = Rand.Range(0.275f, 0.525f);

            lifeTicks = Rand.Range(
                MinLifeTicks,
                MaxLifeTicks);

            fadeInTicks = Rand.Range(15, 31);
            fadeOutTicks = Rand.Range(20, 41);

            if (staggered)
            {
                int maxStartAge = lifeTicks - fadeOutTicks;

                age = Rand.Range(
                    0,
                    Mathf.Max(1, maxStartAge));
            }
            else
            {
                age = 0;
            }

            frame = Rand.Range(0, FrameCount);
            frameTick = Rand.Range(0, FrameInterval);

            initialized = true;
        }

        protected override void Tick()
        {
            if (controller == null
                || controller.Destroyed
                || !controller.Spawned
                || controller.pawn == null)
            {
                Destroy();
                return;
            }

            Pawn pawn = controller.pawn;

            if (pawn.Dead || !pawn.Spawned || pawn.Map == null)
            {
                Destroy();
                return;
            }

            Vector3 center = controller.GetCenter();

            Vector3 centerDelta = center - lastCenter;

            pos.x += centerDelta.x;
            pos.z += centerDelta.z;

            lastCenter = center;

            if (!initialized)
            {
                Initialize(center, true);
                SyncMapPosition();
                return;
            }

            age++;

            if (age >= lifeTicks)
            {
                Initialize(center, false);
                SyncMapPosition();
                return;
            }

            frameTick++;

            if (frameTick >= FrameInterval)
            {
                frameTick = 0;
                frame = (frame + 1) % FrameCount;
            }

            float dx = center.x - pos.x;
            float dz = center.z - pos.z;

            float distanceSquared = dx * dx + dz * dz;
            float distance = Mathf.Sqrt(distanceSquared);

            Vector3 inward = Vector3.zero;

            if (distance > 0.001f)
            {
                inward = new Vector3(
                    dx / distance,
                    0f,
                    dz / distance);

                float speedMultiplier = Mathf.Clamp(
                    controller.speedMultiplier,
                    0.01f,
                    5f);

                float gravity = GravityStrength
                    / Mathf.Max(0.75f, distanceSquared);

                velocity += inward * gravity * speedMultiplier;

                if (distance < 0.8f)
                {
                    velocity -= inward * 0.006f * speedMultiplier;
                }

                if (distance > MaxOrbitRadius)
                {
                    float extraDistance =
                        distance - MaxOrbitRadius;

                    velocity += inward
                        * (extraDistance * 0.0015f)
                        * Mathf.Clamp(controller.speedMultiplier, 0.01f, 5f);
                }
            }

            if (age % 8 == 0)
            {
                float speedMultiplier = Mathf.Clamp(
                    controller.speedMultiplier,
                    0.01f,
                    5f);

                velocity.x += Rand.Range(-0.0010f, 0.0010f)
                    * speedMultiplier;
                velocity.z += Rand.Range(-0.0010f, 0.0010f)
                    * speedMultiplier;
            }

            pos += velocity;

            float movedDx = pos.x - center.x;
            float movedDz = pos.z - center.z;
            float movedDistance = Mathf.Sqrt(
                movedDx * movedDx + movedDz * movedDz);

            if (movedDistance > MaxOrbitRadius && movedDistance > 0.001f)
            {
                float clampScale = MaxOrbitRadius / movedDistance;
                pos.x = center.x + movedDx * clampScale;
                pos.z = center.z + movedDz * clampScale;

                float outwardX = velocity.x * movedDx + velocity.z * movedDz;
                if (outwardX > 0f)
                {
                    float normalizer = movedDistance * movedDistance;
                    velocity.x -= movedDx * outwardX / normalizer;
                    velocity.z -= movedDz * outwardX / normalizer;
                }
            }

            SyncMapPosition();
        }

        private void SyncMapPosition()
        {
            if (!Spawned || Map == null)
                return;

            IntVec3 targetCell = pos.ToIntVec3();

            if (!targetCell.InBounds(Map))
            {
                if (controller != null
                    && controller.pawn != null
                    && controller.pawn.Spawned
                    && controller.pawn.Map == Map)
                {
                    targetCell = controller.pawn.Position;
                }
                else
                {
                    targetCell = Position;
                }
            }

            if (Position != targetCell)
                Position = targetCell;
        }

        private float GetAlpha()
        {
            float fadeIn = Mathf.Clamp01(
                (float)age / Mathf.Max(1, fadeInTicks));

            int fadeOutStart = lifeTicks - fadeOutTicks;

            float fadeOut = 1f;

            if (age > fadeOutStart)
            {
                fadeOut = Mathf.Clamp01(
                    (float)(lifeTicks - age)
                    / Mathf.Max(1, fadeOutTicks));
            }

            return Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
        }

        protected override void DrawAt(
            Vector3 drawLoc,
            bool flip = false)
        {
            if (!initialized)
                return;

            float alpha = GetAlpha();

            if (alpha <= 0.001f)
                return;

            string texPath =
                "Other/ghost/spellBulletAa"
                + frame.ToString("D3");

            Material mat = MaterialPool.MatFrom(
                texPath,
                ShaderDatabase.MoteGlow);

            propertyBlock.SetColor(
                "_Color",
                new Color(1f, 1f, 1f, alpha));

            Vector3 drawPos = pos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                new Vector3(size, 1f, size * 2f));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0,
                null,
                0,
                propertyBlock);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(
                ref controller,
                "controller");

            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref lifeTicks, "lifeTicks", 240);

            Scribe_Values.Look(
                ref fadeInTicks,
                "fadeInTicks",
                20);

            Scribe_Values.Look(
                ref fadeOutTicks,
                "fadeOutTicks",
                30);

            Scribe_Values.Look(ref frame, "frame", 0);
            Scribe_Values.Look(ref frameTick, "frameTick", 0);

            Scribe_Values.Look(
                ref orbitDirection,
                "orbitDirection",
                1);

            Scribe_Values.Look(ref size, "size", 0.8f);

            Scribe_Values.Look(
                ref pos,
                "pos",
                Vector3.zero);

            Scribe_Values.Look(
                ref velocity,
                "velocity",
                Vector3.zero);

            Scribe_Values.Look(
                ref lastCenter,
                "lastCenter",
                Vector3.zero);

            Scribe_Values.Look(
                ref initialized,
                "initialized",
                false);
        }
    }
}