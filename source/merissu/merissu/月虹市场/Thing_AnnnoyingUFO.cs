using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Thing_AnnnoyingUFO : Thing
    {
        private Pawn caster;
        private float orbitAngle;
        private Vector3 currentPos;

        private int interceptedCount = 0;
        private int currentPhase = 0; 

        private const float OrbitRadius = 1.5f;
        private const float OrbitSpeed = 2.0f;
        private const int AnimationFrameInterval = 3;
        private const float InterceptRadius = 0.9f;
        private const float LerpSpeed = 0.15f;

        private static readonly string[][] TexPathsByPhase = {
            new[] { "Accessory/UFO/blue000", "Accessory/UFO/blue001", "Accessory/UFO/blue002", "Accessory/UFO/blue003" },
            new[] { "Accessory/UFO/green000", "Accessory/UFO/green001", "Accessory/UFO/green002", "Accessory/UFO/green003" },
            new[] { "Accessory/UFO/red000", "Accessory/UFO/red001", "Accessory/UFO/red002", "Accessory/UFO/red003" },
            new[] { "Accessory/UFO/yellow000", "Accessory/UFO/yellow001", "Accessory/UFO/yellow002", "Accessory/UFO/yellow003" }
        };

        private static List<Material>[] cachedMatsByPhase;

        public void Init(Pawn pawn)
        {
            this.caster = pawn;
            this.currentPos = pawn.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();
            if (caster == null || !caster.Spawned || caster.Dead)
            {
                if (!Destroyed) Destroy();
                return;
            }

            orbitAngle = (orbitAngle + OrbitSpeed) % 360f;
            UpdatePosition();
            InterceptInRadius();
        }

        private void UpdatePosition()
        {
            float angleRad = orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angleRad) * OrbitRadius, 0, Mathf.Sin(angleRad) * OrbitRadius);
            Vector3 targetPos = caster.DrawPos + offset;
            currentPos = Vector3.Lerp(currentPos, targetPos, LerpSpeed);
        }

        private void InterceptInRadius()
        {
            if (caster?.Map == null) return;

            IntVec3 centerCell = currentPos.ToIntVec3();
            foreach (IntVec3 adjCell in GenAdj.OccupiedRect(centerCell, Rot4.North, new IntVec2(1, 1)).ExpandedBy(1))
            {
                List<Thing> cellThings = adjCell.GetThingList(caster.Map);
                for (int i = cellThings.Count - 1; i >= 0; i--)
                {
                    Thing t = cellThings[i];
                    if (t.def.category == ThingCategory.Projectile)
                    {
                        float sqrDistance = (t.DrawPos - currentPos).sqrMagnitude;
                        if (sqrDistance <= InterceptRadius * InterceptRadius)
                        {
                            RimWorld.EffecterDefOf.Interceptor_BlockedProjectile.Spawn(t.Position, caster.Map).Cleanup();
                            SoundDefOf.Click.PlayOneShot(new TargetInfo(t.Position, caster.Map));
                            t.Destroy();

                            OnIntercepted();
                        }
                    }
                }
            }
        }

        private void OnIntercepted()
        {
            interceptedCount++;
            if (interceptedCount >= 10)
            {
                interceptedCount = 0; 
                ApplyPhaseEffect();
                currentPhase = (currentPhase + 1) % 4; 
            }
        }

        private void ApplyPhaseEffect()
        {
            if (caster == null) return;

            switch (currentPhase)
            {
                case 0: 
                    PlaySoundByName("se_ok00");
                    AddOrUpdateHediff("spiritualpower", 0.5f);
                    break;
                case 1: 
                    PlaySoundByName("powerup");
                    AddOrUpdateHediff("FullPower", 1f);
                    break;
                case 2: 
                    PlaySoundByName("up");
                    AddOrUpdateHediff("up", 1f);
                    break;
                case 3: 
                    PlaySoundByName("MR_OgenticJadeRabbit");
                    AddOrUpdateHediff("spiritualpower", 0.5f);
                    AddOrUpdateHediff("FullPower", 1f);
                    AddOrUpdateHediff("up", 1f);
                    break;
            }
        }

        private void AddOrUpdateHediff(string defName, float amount)
        {
            HediffDef def = HediffDef.Named(defName);
            if (def == null) return;

            Hediff existing = caster.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                existing.Severity += amount;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(def, caster);
                newHediff.Severity = amount;
                caster.health.AddHediff(newHediff);
            }
        }

        private void PlaySoundByName(string defName)
        {
            SoundDef sound = SoundDef.Named(defName);
            sound?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = currentPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(1f, 1f, 1f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, GetCurrentFrameMat(), 0);
        }

        private Material GetCurrentFrameMat()
        {
            if (cachedMatsByPhase == null)
            {
                cachedMatsByPhase = new List<Material>[4];
                for (int p = 0; p < 4; p++)
                {
                    cachedMatsByPhase[p] = new List<Material>();
                    foreach (var path in TexPathsByPhase[p])
                    {
                        cachedMatsByPhase[p].Add(MaterialPool.MatFrom(path, ShaderDatabase.TransparentPostLight));
                    }
                }
            }

            List<Material> currentMats = cachedMatsByPhase[currentPhase];
            int frameIndex = (Find.TickManager.TicksGame / AnimationFrameInterval) % currentMats.Count;
            return currentMats[frameIndex];
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref orbitAngle, "orbitAngle");
            Scribe_Values.Look(ref currentPos, "currentPos");
            Scribe_Values.Look(ref interceptedCount, "interceptedCount");
            Scribe_Values.Look(ref currentPhase, "currentPhase");
        }
    }
}