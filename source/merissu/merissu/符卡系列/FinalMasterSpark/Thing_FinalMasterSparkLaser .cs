using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup] 
    public class Thing_FinalMasterSparkLaser : Thing
    {
        public Vector3 direction = Vector3.forward;
        public Pawn caster;
        private int age;

        private const int TicksPerFrame = 2;
        private const int TotalFrames = 25;

        private const int PhaseOneDuration = 150;
        private const int PhaseTwoDuration = 150;
        private const int PhaseThreeDuration = 60;

        private int PhaseTwoEndTime => PhaseOneDuration + PhaseTwoDuration;
        private int TotalDuration => PhaseTwoEndTime + PhaseThreeDuration;

        private const float SmallHeadSize = 5f;
        private const float BigHeadSize = 25f;
        private const int SmallCollisionRadius = 2;
        private const int BigCollisionRadius = 12;

        private const float BeamLength = 250f;
        private const float GapFiller = 0.004f;

        private const float DamagePerHit = 200f;
        private const float FireChance = 0.5f;
        private const int DamageIntervalTicks = 4;

        private const int TotalFramesEb = 29;
        private static readonly Material[] MatsEb = new Material[TotalFramesEb];
        private static MaterialPropertyBlock _ebPropBlock = new MaterialPropertyBlock();
        private const int EbFadeTicks = 20; 

        private static Mesh _headMesh = null;
        public static Mesh HeadMesh
        {
            get
            {
                if (_headMesh == null)
                {
                    _headMesh = new Mesh { name = "MasterSpark_HeadMesh" };
                    _headMesh.vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                    _headMesh.uv = new Vector2[] { new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.01f), new Vector2(0.01f, 0.99f), new Vector2(0.99f, 0.99f) };
                    _headMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _headMesh.RecalculateNormals();
                }
                return _headMesh;
            }
        }

        private static Mesh _lineMesh = null;
        public static Mesh LineMesh
        {
            get
            {
                if (_lineMesh == null)
                {
                    _lineMesh = new Mesh { name = "MasterSpark_LineMesh" };
                    _lineMesh.vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                    _lineMesh.uv = new Vector2[] { new Vector2(0.98f, 0.01f), new Vector2(0.98f, 0.01f), new Vector2(0.98f, 0.99f), new Vector2(0.98f, 0.99f) };
                    _lineMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                    _lineMesh.RecalculateNormals();
                }
                return _lineMesh;
            }
        }

        static Thing_FinalMasterSparkLaser()
        {
            for (int i = 0; i < TotalFramesEb; i++)
            {
                MatsEb[i] = MaterialPool.MatFrom($"Other/FinalMasterSpark/bulletEb{i:D3}", ShaderDatabase.MoteGlow);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed)
            {
                this.Destroy();
                return;
            }

            this.Position = caster.Position;
            caster.pather.StopDead();
            float shotAngle = direction.AngleFlat();
            caster.Rotation = Rot4.FromAngleFlat(shotAngle);
            if (caster.CurJob != null) caster.CurJob.targetA = caster.Position + (direction * 10f).ToIntVec3();

            if (!(caster.stances.curStance is Stance_FinalMasterSpark))
            {
                caster.stances.SetStance(new Stance_FinalMasterSpark(10, caster.Position + (direction * 5f).ToIntVec3(), null));
            }

            bool isPhaseOne = age <= PhaseOneDuration;
            bool isPhaseTwo = age > PhaseOneDuration && age <= PhaseTwoEndTime;
            bool isPhaseThree = age > PhaseTwoEndTime;

            if (age == PhaseOneDuration + 1)
            {
                SoundDef.Named("FinalMasterSpark").PlayOneShot(new TargetInfo(caster.Position, caster.Map));
                Find.CameraDriver.shaker.DoShake(1.0f);
            }

            if (age % DamageIntervalTicks == 0)
            {
                float currentRadius = 0f;
                if (isPhaseOne) currentRadius = SmallCollisionRadius;
                else if (isPhaseTwo) currentRadius = BigCollisionRadius;
                else
                {
                    float progress = (float)(age - PhaseTwoEndTime) / PhaseThreeDuration;
                    currentRadius = Mathf.Lerp(BigCollisionRadius, 0f, progress);
                }

                if (currentRadius >= 0.5f) DoLaserCollisionAndDamage((int)currentRadius);

                if (Find.CurrentMap == this.Map)
                {
                    float shake = isPhaseOne ? 0.1f : (isPhaseThree ? 0.1f : 0.4f);
                    Find.CameraDriver.shaker.DoShake(shake);
                }
            }

            if (age > TotalDuration) Destroy();
        }

        private void DoLaserCollisionAndDamage(int radius)
        {
            Map map = this.Map;
            Vector3 normDir = direction.normalized;
            Vector3 startPos = caster.DrawPos + (normDir * 0.5f);
            Vector3 endPos = startPos + (normDir * BeamLength);

            HashSet<IntVec3> affectedCells = new HashSet<IntVec3>();
            Vector3 ortho = new Vector3(-normDir.z, 0, normDir.x).normalized;
            float dist = Vector3.Distance(startPos, endPos);

            for (float d = 0; d <= dist; d += 1.0f)
            {
                Vector3 centerPoint = startPos + normDir * d;
                for (int i = -radius; i <= radius; i++)
                {
                    IntVec3 c = (centerPoint + ortho * i).ToIntVec3();
                    if (c.InBounds(map)) affectedCells.Add(c);
                }
            }

            IntVec3 casterPos = caster.Position;

            foreach (IntVec3 cell in affectedCells)
            {
                if (cell == casterPos) continue;

                if (Rand.Value < FireChance) FireUtility.TryStartFireIn(cell, map, 0.6f, this);

                GenTemperature.PushHeat(cell, map, 100f);

                List<Thing> thingList = cell.GetThingList(map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    if (t != this && t != caster && (t is Pawn || t is Building || t.def.useHitPoints))
                    {
                        float damageAmt = DamagePerHit;
                        if (t is Building) damageAmt *= 12f;
                        else if (!(t is Pawn)) damageAmt *= 2f;
                        t.TakeDamage(new DamageInfo(DamageDefOf.Flame, damageAmt, 1.5f, -1f, this));
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 actualLoc = (caster != null) ? caster.DrawPos : drawLoc;
            Vector3 normDir = direction.normalized;
            Quaternion rotation = Quaternion.LookRotation(normDir) * Quaternion.Euler(0, -90, 0);

            if (age <= PhaseOneDuration + EbFadeTicks)
            {
                float baseAlpha = 0.6f; 
                float ebAlpha = baseAlpha;
                float ebExtraScale = 1f;

                if (age > PhaseOneDuration)
                {
                    float fadeProgress = (float)(age - PhaseOneDuration) / EbFadeTicks;
                    ebAlpha = baseAlpha * (1f - fadeProgress);
                    ebExtraScale = 1f + fadeProgress * 0.5f;
                }

                int ebFrame = (age % (TicksPerFrame * TotalFramesEb)) / TicksPerFrame;
                _ebPropBlock.SetColor(ShaderPropertyIDs.Color, new Color(1, 1, 1, ebAlpha));
                DrawEbEffect(actualLoc, normDir, rotation, 0f, 25f * ebExtraScale, ebFrame);
                DrawEbEffect(actualLoc, normDir, rotation, 4f, 35f * ebExtraScale, ebFrame);
            }

            int totalTicksInLoop = TicksPerFrame * TotalFrames;
            int frame = (age % totalTicksInLoop) / TicksPerFrame;
            Material mat = MaterialPool.MatFrom($"Other/MasterSpark/bulletEa{frame:D3}", ShaderDatabase.MoteGlow);

            float currentSize;
            if (age <= PhaseOneDuration) currentSize = SmallHeadSize;
            else if (age <= PhaseTwoEndTime) currentSize = BigHeadSize;
            else
            {
                float progress = (float)(age - PhaseTwoEndTime) / PhaseThreeDuration;
                currentSize = Mathf.Lerp(BigHeadSize, 0f, progress);
            }

            float dynamicOffset = (currentSize / 2f) + 0.5f;
            Vector3 originPos = actualLoc + (normDir * dynamicOffset);
            originPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.8f;

            Matrix4x4 headMatrix = Matrix4x4.TRS(originPos, rotation, new Vector3(currentSize, 1f, currentSize));
            Graphics.DrawMesh(HeadMesh, headMatrix, mat, 0);

            float offsetToBeamCenter = (currentSize / 2f) + (BeamLength / 2f) - GapFiller;
            Vector3 beamPos = originPos + (normDir * offsetToBeamCenter);
            beamPos.y = originPos.y;

            Matrix4x4 beamMatrix = Matrix4x4.TRS(beamPos, rotation, new Vector3(BeamLength, 1f, currentSize));
            Graphics.DrawMesh(LineMesh, beamMatrix, mat, 0);
        }

        private void DrawEbEffect(Vector3 actualLoc, Vector3 normDir, Quaternion rotation, float dist, float size, int frame)
        {
            Vector3 ebPos = actualLoc + (normDir * (dist + size / 2f));
            ebPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.85f; 

            Matrix4x4 matrix = Matrix4x4.TRS(ebPos, rotation, new Vector3(size, 0.1f, size * 1f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MatsEb[frame], 0, null, 0, _ebPropBlock);
        }
    }
}