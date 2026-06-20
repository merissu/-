using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class Thing_StarlightLaserBeam : Thing
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

        private float startBaseAngle;
        private float endBaseAngle;
        private float uvOffset;

        private int fadeTick;
        private const int FadeDuration = 30;

        private const float FlySpeed = 1.2f;
        private const float PointScale = 2f;
        private const float BeamThickness = 1.0f;

        private const int StartParticleInterval = 3;
        private const int EndParticleInterval = 3;
        private const int ParticlesPerBurst = 2;

        private const float AngleJumpRange = 180f;
        private const float UVScrollSpeed = 0.04f;

        private const int DamageInterval = 1;
        private const float DamageAmount = 10f;
        private const float ArmorPenetration = 1f;

        private const float FireChance = 0.65f;
        private const float FireSize = 0.8f;

        public void Init(Vector3 start, Vector3 dir, Pawn instigator)
        {
            startPos = start;
            endPos = start;
            direction = dir.normalized;

            instigatorPawn = instigator;
            startCell = start.ToIntVec3();

            pointMat = MaterialPool.MatFrom(
                "Other/bulletCa000",
                ShaderDatabase.MoteGlow
            );

            beamMat = MaterialPool.MatFrom(
                "Other/bulletCc000",
                ShaderDatabase.MoteGlow
            );

            startBaseAngle = Rand.Range(0f, 360f);
            endBaseAngle = Rand.Range(0f, 360f);

            if (beamMesh == null)
            {
                Mesh src = MeshPool.plane10;
                beamMesh = new Mesh
                {
                    name = "StarlightLaserBeamMesh_Custom",
                    vertices = src.vertices,
                    triangles = src.triangles,
                    uv = src.uv,
                    normals = src.normals
                };

                beamMesh.bounds = new Bounds(
                    Vector3.zero,
                    new Vector3(20000f, 20000f, 20000f)
                );
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

            if (!stopped)
            {
                startParticleTick++;
                if (startParticleTick >= StartParticleInterval)
                {
                    startParticleTick = 0;
                    SpawnParticles(startPos);
                }
            }

            if (!stopped)
            {
                Vector3 nextEnd = endPos + direction * FlySpeed;

                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(
                    endPos.ToIntVec3(),
                    nextEnd.ToIntVec3()
                ))
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
                if (endParticleTick >= EndParticleInterval)
                {
                    endParticleTick = 0;
                    SpawnParticles(endPos);
                }
            }
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
                c.y = AltitudeLayer.Weather.AltitudeFor();
                return c;
            }
        }

        public override void Print(SectionLayer layer)
        {
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float fadeT = stopped
                ? 1f - fadeTick / (float)FadeDuration
                : 1f;

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
                Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(0f, angle, 0f),
                    Vector3.one * PointScale * scaleMul
                ),
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
            block.SetVector("_MainTex_ST",
                new Vector4(1f, length, 0f, uvOffset));

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                rot,
                new Vector3(BeamThickness * scaleMul, 1f, length)
            );

            Graphics.DrawMesh(
                beamMesh,
                matrix,
                beamMat,
                0,
                null,
                0,
                block
            );
        }

        private void ApplyBeamEffects()
        {
            damageTick++;
            bool doDamage = damageTick >= DamageInterval;
            if (doDamage) damageTick = 0;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(
                startPos.ToIntVec3(),
                endPos.ToIntVec3()
            ))
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
                    FireUtility.TryStartFireIn(
                        cell,
                        Map,
                        FireSize,
                        instigatorPawn
                    );
                }
            }
        }

        private void SpawnParticles(Vector3 pos)
        {
            if (Map == null) return;

            for (int i = 0; i < ParticlesPerBurst; i++)
            {
                Thing t = ThingMaker.MakeThing(
                    DefDatabase<ThingDef>.GetNamed("StarlightLaserParticle")
                );

                if (t is Thing_StarlightLaserParticle p)
                {
                    p.Init(pos);
                    GenSpawn.Spawn(p, pos.ToIntVec3(), Map);
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
}
