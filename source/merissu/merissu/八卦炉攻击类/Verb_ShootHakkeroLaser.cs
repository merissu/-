using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Verb_ShootHakkeroLaser : Verb_ShootStarlightLaser
    {
        private const float StartOffset = 0.5f;

        protected override bool TryCastShot()
        {
            if (CasterPawn == null || CasterPawn.Map == null)
                return false;

            SoundDef sound = SoundDef.Named("Hakkerobiglaser");
            if (sound != null)
                sound.PlayOneShot(new TargetInfo(CasterPawn.Position, CasterPawn.Map));

            Vector3 rawStart = CasterPawn.DrawPos;
            Vector3 dir = (currentTarget.Cell.ToVector3Shifted() - rawStart).normalized;
            Vector3 startPos = rawStart + dir * StartOffset;

            Thing_HakkeroLaserBeam beam = (Thing_HakkeroLaserBeam)ThingMaker.MakeThing(
                ThingDef.Named("HakkeroLaserBeam")
            );
            beam.Init(startPos, dir, CasterPawn);
            GenSpawn.Spawn(beam, startPos.ToIntVec3(), CasterPawn.Map);

            return true;
        }
    }

    public class Thing_HakkeroLaserBeam : Thing
    {
        private Vector3 startPos;
        private Vector3 endPos;
        private Vector3 direction;
        private bool stopped;

        private Pawn instigatorPawn;
        private IntVec3 startCell;

        private Material pointMat;
        private Material beamMat;

        private static Mesh beamMesh;

        private int startParticleTick;
        private int endParticleTick;
        private int damageTick;
        private int waveTick;

        private float startBaseAngle;
        private float endBaseAngle;
        private float uvOffset;

        private int fadeTick;
        private const int FadeDuration = 30;

        private const float FlySpeed = 1.2f;
        private const float PointScale = 2f;
        private const float BeamThickness = 2.5f;

        private const int StartPointInterval = 3;
        private const int EndPointInterval = 3;
        private const int WaveInterval = 5;

        private const float AngleJumpRange = 180f;
        private const float UVScrollSpeed = 0.04f;

        private const int DamageInterval = 1;
        private const float DamageAmount = 10f;
        private const float ArmorPenetration = 1f;

        private const float FireChance = 0.65f;
        private const float FireSize = 0.8f;

        private const string PointTexPath = "Projectiles/MARISA/Hakkerobiglaser/bulletAb000";
        private const string BeamTexPath = "Projectiles/MARISA/Hakkerobiglaser/bulletGa006";

        public void Init(Vector3 start, Vector3 dir, Pawn instigator)
        {
            startPos = start;
            endPos = start;
            direction = dir.normalized;

            instigatorPawn = instigator;
            startCell = start.ToIntVec3();

            pointMat = MaterialPool.MatFrom(PointTexPath, ShaderDatabase.MoteGlow);
            beamMat = MaterialPool.MatFrom(BeamTexPath, ShaderDatabase.MoteGlow);

            startBaseAngle = Rand.Range(0f, 360f);
            endBaseAngle = Rand.Range(0f, 360f);

            if (beamMesh == null)
            {
                Mesh src = MeshPool.plane10;
                beamMesh = new Mesh
                {
                    name = "HakkeroLaserBeamMesh",
                    vertices = src.vertices,
                    triangles = src.triangles,
                    uv = src.uv,
                    normals = src.normals
                };
                beamMesh.bounds = new Bounds(Vector3.zero, new Vector3(20000f, 20000f, 20000f));
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null) return;

            UpdatePositionToBeamCenter();

            if (!stopped)
            {
                uvOffset -= UVScrollSpeed;
                if (uvOffset < 0f) uvOffset += 1f;
            }

            waveTick++;
            if (waveTick >= WaveInterval)
            {
                waveTick = 0;
                SpawnWave(startPos);
            }

            if (!stopped)
            {
                startParticleTick++;
                if (startParticleTick >= StartPointInterval)
                    startParticleTick = 0;
            }

            if (!stopped)
            {
                Vector3 nextEnd = endPos + direction * FlySpeed;
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(endPos.ToIntVec3(), nextEnd.ToIntVec3()))
                {
                    if (!cell.InBounds(Map))
                    {
                        stopped = true;
                        break;
                    }
                    Building b = cell.GetFirstBuilding(Map);
                    if (b != null && b.def.Fillage == FillCategory.Full)
                    {
                        endPos = b.DrawPos;
                        stopped = true;
                        break;
                    }
                }
                if (!stopped)
                    endPos = nextEnd;
            }
            else
            {
                fadeTick++;
                if (fadeTick >= FadeDuration)
                {
                    Destroy();
                    return;
                }
            }

            ApplyBeamEffects();

            if (!stopped || fadeTick < FadeDuration / 2)
            {
                endParticleTick++;
                if (endParticleTick >= EndPointInterval)
                    endParticleTick = 0;
            }
        }

        private void SpawnWave(Vector3 pos)
        {
            ThingDef waveDef = ThingDef.Named("HakkeroLaserWave");
            Thing_HakkeroLaserWave wave = (Thing_HakkeroLaserWave)ThingMaker.MakeThing(waveDef);
            wave.SetWorldPos(pos);
            GenSpawn.Spawn(wave, pos.ToIntVec3(), Map);
        }

        private void UpdatePositionToBeamCenter()
        {
            Vector3 center = (startPos + endPos) * 0.5f;
            IntVec3 cell = center.ToIntVec3();
            if (cell.InBounds(Map))
                Position = cell;
        }

        public override Vector3 DrawPos
        {
            get
            {
                Vector3 c = (startPos + endPos) * 0.5f;
                c.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                return c;
            }
        }

        public override void Print(SectionLayer layer) { }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float fadeT = stopped ? 1f - fadeTick / (float)FadeDuration : 1f;
            fadeT = Mathf.Clamp01(fadeT);

            float startAngle = startBaseAngle + Rand.Range(-AngleJumpRange, AngleJumpRange);
            float endAngle = endBaseAngle + Rand.Range(-AngleJumpRange, AngleJumpRange);

            DrawPoint(startPos, startAngle, fadeT);
            DrawPoint(endPos, endAngle, fadeT);
            DrawBeamUVFlow(fadeT);
        }

        private void DrawPoint(Vector3 pos, float angle, float scaleMul)
        {
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(pos, Quaternion.Euler(0f, angle, 0f), Vector3.one * PointScale * scaleMul),
                pointMat,
                0
            );
        }

        private void DrawBeamUVFlow(float scaleMul)
        {
            Vector3 delta = endPos - startPos;
            float length = delta.magnitude;
            if (length < 0.01f) return;

            Vector3 center = (startPos + endPos) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(delta);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetVector("_MainTex_ST", new Vector4(1f, length, 0f, uvOffset));

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                rot,
                new Vector3(BeamThickness * scaleMul, 1f, length)
            );

            Graphics.DrawMesh(beamMesh, matrix, beamMat, 0, null, 0, block);
        }

        private void ApplyBeamEffects()
        {
            damageTick++;
            bool doDamage = damageTick >= DamageInterval;
            if (doDamage) damageTick = 0;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(startPos.ToIntVec3(), endPos.ToIntVec3()))
            {
                if (!cell.InBounds(Map)) continue;
                if (cell == startCell) continue;

                Pawn pawn = cell.GetFirstPawn(Map);
                if (pawn == instigatorPawn) continue;

                if (doDamage && pawn != null && !pawn.Dead)
                {
                    pawn.TakeDamage(new DamageInfo(
                        DamageDefOf.Burn,
                        DamageAmount,
                        ArmorPenetration,
                        direction.ToAngleFlat(),
                        instigatorPawn
                    ));
                }

                if (Rand.Value < FireChance)
                {
                    FireUtility.TryStartFireIn(cell, Map, FireSize, instigatorPawn);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startPos, "startPos");
            Scribe_Values.Look(ref endPos, "endPos");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref stopped, "stopped");
            Scribe_Values.Look(ref uvOffset, "uvOffset");
            Scribe_Values.Look(ref fadeTick, "fadeTick");
            Scribe_Values.Look(ref damageTick, "damageTick");
            Scribe_References.Look(ref instigatorPawn, "instigatorPawn");
        }
    }

    public class Thing_HakkeroLaserWave : Thing
    {
        private int age;
        private const int LifeTime = 20;
        private const float StartScale = 0.5f;
        private const float EndScale = 2.5f;
        private const float StartAlpha = 0.5f;

        private Vector3 worldPos = Vector3.zero;

        public void SetWorldPos(Vector3 pos) => worldPos = pos;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= LifeTime)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float t = age / (float)LifeTime;
            float scale = Mathf.Lerp(StartScale, EndScale, t);
            float alpha = Mathf.Lerp(StartAlpha, 0f, t);

            Vector3 pos = worldPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = Graphic.MatSingle;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(scale, 1f, scale)
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0,
                null,
                0,
                block
            );
        }
    }
}