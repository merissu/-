using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_IndraThunderbolt : Ability
    {
        public Ability_IndraThunderbolt() : base() { }
        public Ability_IndraThunderbolt(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null) return false;

            Hediff regenBuff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ShinkiBuff_SpiritualRegen"));
            Hediff recitationBuff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("hijiriShinkiRecitation"));

            if (regenBuff != null && recitationBuff == null)
            {
                pawn.health.RemoveHediff(regenBuff);
            }

            if (def.verbProperties.soundCast != null)
            {
                def.verbProperties.soundCast.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            Thing_IndraBeadCluster cluster = (Thing_IndraBeadCluster)GenSpawn.Spawn(
                ThingDef.Named("Thing_IndraBeadCluster"),
                pawn.Position,
                pawn.Map
            );
            cluster.Initialize(pawn, target.Cell);

            return true;
        }
    }

    public class Thing_IndraBeadCluster : Thing
    {
        private Pawn caster;
        private IntVec3 targetCell;
        private Vector3 currentCenter;
        private Vector3 startPos;
        private Vector3 targetPos;

        private int age = 0;
        private const int OrbitTicks = 36;
        private float orbitRadius = 1.5f;
        private float currentAngle = 0f;
        private float spinSpeed = 10f;
        private float flightSpeed = 0.6f;
        private bool isFlying = false;

        public void Initialize(Pawn caster, IntVec3 target)
        {
            this.caster = caster;
            this.targetCell = target;
            this.startPos = caster.DrawPos;
            this.currentCenter = this.startPos;
            this.targetPos = target.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null) return;
            age++;
            currentAngle += spinSpeed;

            if (!isFlying)
            {
                if (caster != null && !caster.Destroyed && caster.Spawned)
                {
                    currentCenter = caster.DrawPos;
                }

                if (age >= OrbitTicks)
                {
                    isFlying = true;
                    startPos = currentCenter;
                }
            }
            else
            {
                Vector3 dir = (targetPos - currentCenter).normalized;
                float dist = Vector3.Distance(currentCenter, targetPos);

                if (dist <= flightSpeed)
                {
                    currentCenter = targetPos;
                    Position = targetCell;
                    Impact();
                    Destroy();
                    return;
                }

                currentCenter += dir * flightSpeed;
                Position = currentCenter.ToIntVec3();

                float totalDist = Vector3.Distance(startPos, targetPos);
                if (totalDist > 0.001f)
                {
                    float progress = 1f - (dist / totalDist);
                    orbitRadius = Mathf.Lerp(1.5f, 0f, progress);
                }
            }
        }

        private void Impact()
        {
            Thing_IndraImpactFlash flash = (Thing_IndraImpactFlash)GenSpawn.Spawn(
                ThingDef.Named("Thing_IndraImpactFlash"),
                targetCell,
                Map
            );
            flash.Initialize(targetPos);

            Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, targetCell));

            foreach (IntVec3 offset in GenAdj.AdjacentCells)
            {
                IntVec3 adjCell = targetCell + offset;
                if (adjCell.InBounds(Map))
                {
                    Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, adjCell));
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Material beadMat = MaterialPool.MatFrom("Other/ShinkiRecitation/stoneA0000", ShaderDatabase.Mote);
            if (beadMat == null) return;

            int beadCount = 8;
            float angleStep = 360f / beadCount;

            for (int i = 0; i < beadCount; i++)
            {
                float angle = -(currentAngle + (i * angleStep)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * orbitRadius, 0f, Mathf.Sin(angle) * orbitRadius);
                Vector3 beadPos = currentCenter + offset;
                beadPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                Matrix4x4 matrix = Matrix4x4.TRS(beadPos, Quaternion.identity, new Vector3(1.5f, 1f, 1.5f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, beadMat, 0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref currentCenter, "currentCenter");
            Scribe_Values.Look(ref startPos, "startPos");
            Scribe_Values.Look(ref targetPos, "targetPos");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref orbitRadius, "orbitRadius");
            Scribe_Values.Look(ref currentAngle, "currentAngle");
            Scribe_Values.Look(ref isFlying, "isFlying");
        }
    }

    public class Thing_IndraImpactFlash : Thing
    {
        private Vector3 exactPos;
        private int age = 0;
        private const int MaxAge = 25;

        public void Initialize(Vector3 pos)
        {
            this.exactPos = pos;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float percent = (float)age / MaxAge;

            Material coreMat = MaterialPool.MatFrom("Other/ShinkiRecitation/climaxLaserCoreB", ShaderDatabase.MoteGlow);
            if (coreMat != null)
            {
                float coreAlpha = 1.0f - percent;
                Material fadedCore = FadedMaterialPool.FadedVersionOf(coreMat, coreAlpha);
                float coreScale = Mathf.Lerp(1.0f, 3.5f, percent);

                Vector3 pos = exactPos;
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(coreScale, 1f, coreScale));
                Graphics.DrawMesh(MeshPool.plane10, matrix, fadedCore, 0);
            }

            Material outerMat = MaterialPool.MatFrom("Other/ShinkiRecitation/climaxLaserCoreA", ShaderDatabase.MoteGlow);
            if (outerMat != null)
            {
                float outerAlpha = Mathf.Clamp01(1.0f - (percent * 1.8f));
                if (outerAlpha > 0f)
                {
                    Material fadedOuter = FadedMaterialPool.FadedVersionOf(outerMat, outerAlpha);
                    Vector3 pos = exactPos;
                    pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.001f;

                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(3f, 1f, 3f));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, fadedOuter, 0);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exactPos, "exactPos");
            Scribe_Values.Look(ref age, "age");
        }
    }
}