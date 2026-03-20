using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound; 

namespace merissu
{
    public class Thing_CorollaVisionRing : Thing
    {
        private Pawn caster;
        private Vector3 exactPos;
        private Vector3 direction;
        private float rotation;
        private float speed = 0.35f;
        private float traveled;
        private float alpha = 1f;

        private const float MaxDistance = 25f;
        private const float FadeStartDistance = 18f;
        private const float GrowDistance = 5f;
        private const float TrailStopDistance = 15f;

        private const float TrailSpacing = 2f;
        private float nextTrailDist;
        private const int MaxTrailCount = 5;

        public void Init(Pawn caster, Vector3 start, Vector3 dir)
        {
            this.caster = caster;
            this.exactPos = start;
            this.direction = dir;
            this.rotation = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            this.nextTrailDist = TrailSpacing;

            SoundDef visionSound = SoundDef.Named("CorollaVision");
            if (visionSound != null)
            {
                visionSound.PlayOneShot(new TargetInfo(start.ToIntVec3(), caster.Map));
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null) return;

            exactPos += direction * speed;
            traveled += speed;

            SpawnTrailIfNeeded();
            CheckHitPawns();
            UpdateFade();

            if (alpha <= 0f) Destroy();
        }

        private void SpawnTrailIfNeeded()
        {
            if (traveled > TrailStopDistance) return;
            if (traveled < nextTrailDist) return;

            int currentCount = 0;
            List<Thing> allThings = Map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                if (allThings[i] is Thing_CorollaVisionTrail && !allThings[i].Destroyed)
                {
                    currentCount++;
                }
            }

            if (currentCount >= MaxTrailCount) return;

            nextTrailDist += TrailSpacing;

            Thing_CorollaVisionTrail trail = (Thing_CorollaVisionTrail)ThingMaker.MakeThing(ThingDef.Named("CorollaVisionTrail"));
            trail.Init(exactPos, rotation, CurrentRadius);

            GenSpawn.Spawn(trail, exactPos.ToIntVec3(), Map);
        }

        private void UpdateFade()
        {
            if (traveled < FadeStartDistance) return;
            float fadeProgress = (traveled - FadeStartDistance) / (MaxDistance - FadeStartDistance);
            alpha = Mathf.Clamp01(1f - fadeProgress);
        }

        private void CheckHitPawns()
        {
            float radius = CurrentRadius;
            foreach (Pawn p in Map.mapPawns.AllPawnsSpawned)
            {
                if (p == caster || p.Faction == caster.Faction || p.Dead || p.Downed) continue;
                if (Vector3.Distance(p.DrawPos, exactPos) <= radius)
                {
                    if (p.mindState.mentalStateHandler.CurStateDef != MentalStateDefOf.Berserk)
                    {
                        p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Corolla Vision", forceWake: true);
                    }
                }
            }
        }

        private float CurrentRadius
        {
            get
            {
                if (traveled >= GrowDistance) return 1.5f;
                return Mathf.Lerp(0.5f, 1.5f, traveled / GrowDistance);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float size = CurrentRadius * 2f;
            Vector3 drawPos = exactPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.Euler(0f, rotation, 0f),
                new Vector3(size, 1f, size));

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, mpb);
        }
    }
}