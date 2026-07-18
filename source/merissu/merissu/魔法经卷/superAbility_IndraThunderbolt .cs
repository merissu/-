using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class superAbility_IndraThunderbolt : Ability
    {
        public superAbility_IndraThunderbolt() : base() { }
        public superAbility_IndraThunderbolt(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null) return false;


            if (def.verbProperties.soundCast != null)
            {
                def.verbProperties.soundCast.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            Thing_SuperIndraBeadCluster cluster = (Thing_SuperIndraBeadCluster)GenSpawn.Spawn(
                ThingDef.Named("Thing_SuperIndraBeadCluster"),
                pawn.Position,
                pawn.Map
            );
            cluster.Initialize(pawn, target.Cell);

            return true;
        }
    }

    public class Thing_SuperIndraBeadCluster : Thing
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

        private bool striking = false;              
        private int strikeCount = 0;                
        private const int TotalStrikes = 3;         
        private const int StrikeIntervalTicks = 15; 
        private int strikeTimer = 0;

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

            if (!isFlying && !striking)
            {
                currentAngle += spinSpeed;
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
            else if (isFlying && !striking)
            {
                Vector3 dir = (targetPos - currentCenter).normalized;
                float dist = Vector3.Distance(currentCenter, targetPos);

                if (dist <= flightSpeed)
                {
                    currentCenter = targetPos;
                    Position = targetCell;
                    isFlying = false;
                    striking = true;
                    strikeCount = 0;
                    strikeTimer = 0;
                    DoStrike();
                    strikeCount++;
                }
                else
                {
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
            else if (striking)
            {
                strikeTimer++;
                if (strikeCount < TotalStrikes && strikeTimer >= StrikeIntervalTicks)
                {
                    DoStrike();
                    strikeCount++;
                    strikeTimer = 0;
                }

                if (strikeCount >= TotalStrikes && strikeTimer >= 10)
                {
                    Destroy();
                }
            }
        }

        private void DoStrike()
        {
            if (Map == null) return;

            Thing_IndraImpactFlash flash = (Thing_IndraImpactFlash)GenSpawn.Spawn(
                ThingDef.Named("Thing_IndraImpactFlash"),
                targetCell,
                Map
            );
            flash.Initialize(targetPos);

            float radius = 3f;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, radius, true))
            {
                if (cell.InBounds(Map))
                {
                    Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, cell));
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
            Scribe_Values.Look(ref striking, "striking");
            Scribe_Values.Look(ref strikeCount, "strikeCount");
            Scribe_Values.Look(ref strikeTimer, "strikeTimer");
        }
    }
}