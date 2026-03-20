using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class Thing_HisoutenLaser : Thing
    {
        public Vector3 direction;
        public Pawn caster;

        private int age = 0;
        private float beamLength = 0f;

        private const int StartupFrames = 10;
        private const int LoopFrames = 9;
        private const int TicksPerFrame = 3;
        private const float AspectRatio = 0.5f;

        private bool isFinishing = false;
        private int finishStartAge = 0;
        private const int FinishDuration = 20;
        private const int MaxLoopDuration = 180;
        private const int DamageIntervalTicks = 10; 
        private const float DamageAmount = 50f;     
        private const float SectorAngle = 15f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            beamLength = CalculateDistanceToEdge(this.Position.ToVector3(), direction) + 5f;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (!isFinishing)
            {
                if (age % DamageIntervalTicks == 0)
                {
                    DoSectorDamage();
                }

                if (!isFinishing)
                {
                    if (age % DamageIntervalTicks == 0)
                    {
                        DoSectorDamage();

                        if (Find.CurrentMap == this.Map)
                        {
                            Find.CameraDriver.shaker.DoShake(0.5f);
                        }
                    }
                    if (age % 8 == 0)
                {
                    ApplyHisoutenStatusToArea();
                }
                if (!isFinishing && age > StartupFrames * TicksPerFrame)
                {
                    if (age % 60 == 0) 
                    {
                        FireProjectilesEveryCellInSector();
                    }
                }
                if (caster == null || !caster.Spawned || caster.Dead || caster.Downed)
                {
                    if (!isFinishing) StopLaser();
                }

                if (caster != null && caster.Spawned && !caster.Dead)
                {
                    caster.pather.StopDead();
                    caster.Rotation = Rot4.FromAngleFlat(direction.AngleFlat());

                    if (!isFinishing)
                    {
                        if (!(caster.stances.curStance is Stance_Hisouten))
                        {
                            IntVec3 targetCell = (caster.Position.ToVector3() + direction * 5f).ToIntVec3();
                            caster.stances.SetStance(new Stance_Hisouten(MaxLoopDuration + 50, targetCell, null));
                        }
                        else
                        {
                            ((Stance_Hisouten)caster.stances.curStance).ticksLeft = 20;
                        }
                    }
                }

                if (!isFinishing)
                {
                    if (age > (StartupFrames + LoopFrames) * TicksPerFrame + MaxLoopDuration)
                    {
                        StopLaser();
                    }
                }
                else if (age - finishStartAge > FinishDuration)
                {
                    this.Destroy();
                }
            }
        }
    }
        private void DoSectorDamage()
        {
            if (this.Map == null || caster == null) return;

            Vector3 center = caster.DrawPos;

            float baseAngle = direction.AngleFlat();

            IReadOnlyList<Pawn> allPawns = this.Map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];

                if (p == caster || p.Dead) continue;

                Vector3 targetPos = p.DrawPos;
                float dist = Vector3.Distance(center, targetPos);

                if (dist > 0 && dist <= beamLength)
                {
                    Vector3 targetDir = (targetPos - center).normalized;
                    float angleToTarget = targetDir.AngleFlat();

                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(baseAngle, angleToTarget));

                    if (angleDiff <= 15f)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, DamageAmount, 0f, -1f, caster);
                        p.TakeDamage(dinfo);

                        FleckMaker.ThrowMicroSparks(targetPos, this.Map);
                    }
                }
            }
        }
        private void FireProjectilesEveryCellInSector()
        {
            if (this.Map == null || caster == null) return;

            Vector3 origin = caster.DrawPos + direction;
            float baseAngle = direction.AngleFlat();

            for (float dist = 0.5f; dist <= beamLength; dist += 1.0f)
            {
                float angleStep = Mathf.Min(2.0f, 40.0f / dist);

                for (float currAngle = -SectorAngle / 2f; currAngle <= SectorAngle / 2f; currAngle += angleStep)
                {
                    if (Rand.Value < 0.3f) continue;

                    float finalAngle = baseAngle + currAngle;
                    Vector3 spawnPos = origin + MathUtils.AngleToVector(finalAngle) * dist;

                    IntVec3 spawnCell = spawnPos.ToIntVec3();
                    if (!spawnCell.InBounds(this.Map)) continue;

                    MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_HisoutenGrain"));

                    if (mote is Mote_HisoutenAnimated animMote)
                    {
                        animMote.myScale = Rand.Range(1f, 1.1f); 
                        animMote.randOffset = Rand.Range(0, 100); 
                    }

                    mote.exactPosition = spawnPos;
                    mote.exactRotation = Rand.Range(0f, 360f);
                    mote.rotationRate = Rand.Range(-3f, 3f);

                    float launchAngle = Rand.Range(0f, 360f);
                    float speed = Rand.Range(2f, 5f);
                    mote.SetVelocity(launchAngle, speed);

                    GenSpawn.Spawn(mote, spawnCell, this.Map);
                }
            }
        }
        private void ApplyHisoutenStatusToArea()
        {
            if (this.Map == null || caster == null) return;
            float currentWidth = beamLength * AspectRatio;
            float halfWidth = currentWidth / 2f;
            Vector3 startCenter = caster.DrawPos;
            Vector3 endCenter = caster.DrawPos + direction * beamLength;
            Vector3 lineVec = endCenter - startCenter;
            float lineLenSq = lineVec.sqrMagnitude;

            System.Collections.Generic.IReadOnlyList<Pawn> candidates = caster.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn p = candidates[i];
                if (p == caster || p.Dead) continue;
                Vector3 pPos = p.DrawPos;
                float t = Vector3.Dot(pPos - startCenter, lineVec) / lineLenSq;
                if (t >= 0 && t <= 1)
                {
                    Vector3 projection = startCenter + t * lineVec;
                    if ((pPos - projection).sqrMagnitude <= halfWidth * halfWidth)
                    {
                        p.health.AddHediff(HediffDef.Named("Hisouten"));
                    }
                }
            }
        }

        public void StopLaser()
        {
            if (isFinishing) return;
            isFinishing = true;
            finishStartAge = age;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;
            int frameIndex;
            int totalStartupTicks = StartupFrames * TicksPerFrame;
            if (age < totalStartupTicks) frameIndex = age / TicksPerFrame;
            else
            {
                int loopAge = age - totalStartupTicks;
                frameIndex = StartupFrames + (loopAge / TicksPerFrame) % LoopFrames;
            }

            Material mat = MaterialPool.MatFrom($"Other/Hisouten/spellBulletCa{frameIndex:D3}", ShaderDatabase.MoteGlow);
            mat.mainTexture.wrapMode = TextureWrapMode.Clamp;
            float alpha = isFinishing ? Mathf.Lerp(1.0f, 0f, (float)(age - finishStartAge) / FinishDuration) : 1.0f;
            if (alpha <= 0.001f) return;

            float angle = direction.AngleFlat();
            Quaternion rotation = Quaternion.Euler(0, angle + 270f, 0);
            Vector3 scale = new Vector3(beamLength, 1f, beamLength * AspectRatio);
            Vector3 finalPos = caster.DrawPos + (direction * (1.5f - beamLength * 0.1f));
            finalPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.7f;

            Matrix4x4 matrix = Matrix4x4.TRS(finalPos, rotation, scale) * Matrix4x4.Translate(new Vector3(0.5f, 0, 0));
            Graphics.DrawMesh(MeshPool.plane10, matrix, FadedMaterialPool.FadedVersionOf(mat, alpha), 0);
        }

        private float CalculateDistanceToEdge(Vector3 startPos, Vector3 dir)
        {
            Map map = this.Map;
            float t = float.MaxValue;
            if (dir.x > 0) t = Mathf.Min(t, (map.Size.x + 100f - startPos.x) / dir.x);
            else if (dir.x < 0) t = Mathf.Min(t, (0 - 100f - startPos.x) / dir.x);
            if (dir.z > 0) t = Mathf.Min(t, (map.Size.z + 100f - startPos.z) / dir.z);
            else if (dir.z < 0) t = Mathf.Min(t, (0 - 100f - startPos.z) / dir.z);
            return t;
        }
    }

    public static class MathUtils
    {
        public static Vector3 AngleToVector(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
        }
    }
}