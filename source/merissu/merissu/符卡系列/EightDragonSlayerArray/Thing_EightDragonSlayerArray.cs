using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class Thing_EightDragonSlayerArrayAnimation : Thing
    {
        private Pawn caster;
        private int age;

        private const int TotalDuration = 300;
        private const int FadeTicks = 40;
        private const int DamageInterval = 3;
        private const float DamageAmount = 3f;
        private const int SquareRadius = 10;
        private const int TicksPerFrame = 2;

        private const int TotalFrames = 28;
        private const int LoopStart = 6;
        private const int LoopEnd = 27;
        private const int IntroFrameCount = 28;

        private const float BeamLength = 250f;
        private const float HeadSize = 8f;
        private const int PixelRowFromTop = 21;

        private const float ScaleMultiplier = 2f * 1.5f;   
        private const float HeightMultiplier = 4f * 1.5f; 

        private const string TexPathPrefix = "Projectiles/EightDragonSlayerArray/spellBulletFa";

        private static Mesh quadMesh;
        private static Mesh QuadMesh
        {
            get
            {
                if (quadMesh == null)
                    quadMesh = MeshMakerPlanes.NewPlaneMesh(1f, false);
                return quadMesh;
            }
        }

        private Mesh beamMesh;
        private float storedVCoord = 1f; 

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (age % DamageInterval == 0)
                ApplyArrayEffects();

            BlockProjectiles();

            if (age >= TotalDuration + FadeTicks)
            {
                Destroy();
                EndAbilityEffects();
            }
        }

        private void EndAbilityEffects()
        {
            if (caster?.health != null)
            {
                Hediff lockHediff = caster.health.hediffSet.GetFirstHediffOfDef(
                    HediffDef.Named("EightDragonCasterLock"));
                if (lockHediff != null)
                    caster.health.RemoveHediff(lockHediff);
            }
        }

        private void ApplyArrayEffects()
        {
            Map map = caster?.Map;
            if (map == null) return;

            IntVec3 center = caster.Position;
            HediffDef burnLockDef = HediffDef.Named("EightDragonBurnLock");

            List<Pawn> targets = map.mapPawns.AllPawnsSpawned.ToList();
            foreach (Pawn target in targets)
            {
                if (target == caster || target.Dead || target.Faction == caster.Faction)
                    continue;

                bool inSquare =
                    Mathf.Abs(target.Position.x - center.x) <= SquareRadius &&
                    Mathf.Abs(target.Position.z - center.z) <= SquareRadius;

                bool inNorthBeam =
                    Mathf.Abs(target.Position.x - center.x) <= SquareRadius &&
                    target.Position.z >= center.z;

                if (inSquare || inNorthBeam)
                {
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Flame,
                        DamageAmount,
                        0f,
                        -1f,
                        caster);
                    target.TakeDamage(dinfo);

                    if (burnLockDef != null && !target.health.hediffSet.HasHediff(burnLockDef))
                        target.health.AddHediff(burnLockDef);
                }
            }
        }

        private void BlockProjectiles()
        {
            Map map = caster?.Map;
            if (map == null) return;

            IntVec3 center = caster.Position;
            for (int x = -SquareRadius; x <= SquareRadius; x++)
            {
                for (int z = -SquareRadius; z <= SquareRadius; z++)
                {
                    IntVec3 cell = center + new IntVec3(x, 0, z);
                    if (!cell.InBounds(map)) continue;

                    List<Thing> things = cell.GetThingList(map).ToList();
                    foreach (Thing t in things)
                    {
                        if (t is Projectile)
                            t.Destroy();
                    }
                }
            }
        }

        private int GetCurrentFrame()
        {
            int rawFrame = age / TicksPerFrame;

            if (rawFrame < IntroFrameCount)
                return rawFrame;

            int loopedFrames = rawFrame - IntroFrameCount;
            int loopLength = LoopEnd - LoopStart + 1;
            int loopIndex = loopedFrames % loopLength;
            return LoopStart + loopIndex;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            float alpha = 1f;
            if (age > TotalDuration)
            {
                float fadeProgress = (age - TotalDuration) / (float)FadeTicks;
                alpha = 1f - Mathf.Clamp01(fadeProgress);
                if (alpha <= 0f) return;
            }

            int frame = GetCurrentFrame();
            string texPath = $"{TexPathPrefix}{frame:D3}";
            Material mat = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow);
            mat.color = new Color(1f, 1f, 1f, alpha);

            if (beamMesh == null && mat.mainTexture != null)
            {
                int texHeight = mat.mainTexture.height;
                storedVCoord = 1f - (PixelRowFromTop / (float)texHeight);
                CreateBeamMesh(storedVCoord);
            }

            Vector3 basePos = caster.DrawPos;
            Vector3 mainPos = basePos;
            mainPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float scaleX = HeadSize * ScaleMultiplier;
            float scaleZ = HeadSize * HeightMultiplier;

            Matrix4x4 mainMatrix = Matrix4x4.TRS(
                mainPos,
                Quaternion.identity,
                new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(QuadMesh, mainMatrix, mat, 0);

            float beamOffsetZ = (storedVCoord - 0.5f) * scaleZ;
            Vector3 beamPos = mainPos + new Vector3(0f, 0f, beamOffsetZ);

            Matrix4x4 beamMatrix = Matrix4x4.TRS(
                beamPos,
                Quaternion.identity,
                new Vector3(scaleX, 1f, BeamLength));
            if (beamMesh != null)
                Graphics.DrawMesh(beamMesh, beamMatrix, mat, 0);
        }

        private void CreateBeamMesh(float vCoord)
        {
            beamMesh = new Mesh { name = "EightDragonBeamMesh" };
            beamMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, 1f),
                new Vector3(0.5f, 0f, 1f)
            };
            beamMesh.uv = new Vector2[]
            {
                new Vector2(0.01f, vCoord),
                new Vector2(0.99f, vCoord),
                new Vector2(0.01f, vCoord),
                new Vector2(0.99f, vCoord)
            };
            beamMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
            beamMesh.RecalculateNormals();
        }
    }
}