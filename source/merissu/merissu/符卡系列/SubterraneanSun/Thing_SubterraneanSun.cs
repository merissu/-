using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;

namespace merissu
{
    public class Thing_SubterraneanSun : Thing
    {
        public IntVec3 centerCell;

        private int age;
        private const int LifetimeTicks = 360;
        private const int FadeOutTicks = 60;

        private const int TicksPerFrame = 3;
        private const int TotalFrames = 15;
        private const float SunRadius = 20f;

        private const int PullInterval = 10;
        private const float PullRadius = 60f;

        private List<SunParticle> particles = new List<SunParticle>();

        #region Mesh
        private static Mesh quadMesh;
        public static Mesh QuadMesh
        {
            get
            {
                if (quadMesh == null)
                {
                    quadMesh = new Mesh();
                    quadMesh.vertices = new Vector3[]
                    {
                        new Vector3(-0.5f, 0, -0.5f),
                        new Vector3( 0.5f, 0, -0.5f),
                        new Vector3(-0.5f, 0,  0.5f),
                        new Vector3( 0.5f, 0,  0.5f)
                    };
                    quadMesh.uv = new Vector2[]
                    {
                        new Vector2(0,0),
                        new Vector2(1,0),
                        new Vector2(0,1),
                        new Vector2(1,1)
                    };
                    quadMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                }
                return quadMesh;
            }
        }
        #endregion

        protected override void Tick()
        {
            base.Tick();

            age++;

            if (age % 2 == 0)
                SpawnParticle();

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Tick();
                if (particles[i].Dead)
                    particles.RemoveAt(i);
            }

            if (age % 1 == 0)
            {
                ApplySunDamage();
            }

            if (age % PullInterval == 0)
            {
                PullHostiles();
            }

            if (age >= LifetimeTicks)
                Destroy();
        }

        private void PullHostiles()
        {
            if (Map == null) return;

            IReadOnlyList<Pawn> allPawns = Map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];

                if (pawn.Dead || pawn.Downed) continue;

                if (!pawn.HostileTo(Faction.OfPlayer)) continue;

                if (pawn.Position.DistanceTo(centerCell) > PullRadius) continue;

                IntVec3 currentPos = pawn.Position;
                IntVec3 direction = centerCell - currentPos;

                IntVec3 moveStep = new IntVec3(
                    Mathf.Clamp(direction.x, -1, 1),
                    0,
                    Mathf.Clamp(direction.z, -1, 1)
                );

                IntVec3 targetPos = currentPos + moveStep;

                if (targetPos.InBounds(Map) && targetPos.Walkable(Map))
                {
                    pawn.Position = targetPos;

                    if (pawn.pather != null)
                    {
                        pawn.pather.StopDead();
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 pos = centerCell.ToVector3Shifted();
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.5f;

            float alpha = 1f;
            if (age > LifetimeTicks - FadeOutTicks)
                alpha = 1f - (float)(age - (LifetimeTicks - FadeOutTicks)) / FadeOutTicks;

            DrawSun(pos, alpha);
            DrawParticles(pos, alpha);
        }

        private void DrawSun(Vector3 pos, float alpha)
        {
            int frame = (age / TicksPerFrame) % TotalFrames;
            Material mat = MaterialPool.MatFrom(
                $"Other/SUN/bulletOa{frame:D3}",
                ShaderDatabase.MoteGlow
            );

            mat.color = new Color(1f, 1f, 1f, alpha);
            mat.renderQueue = 4000;

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                Vector3.one * SunRadius * 2f
            );

            Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
        }

        private void SpawnParticle()
        {
            particles.Add(new SunParticle
            {
                angle = Rand.Range(0f, 360f),
                sizeStart = Rand.Range(0.6f, 1.2f),
                sizeEnd = Rand.Range(2.5f, 3.5f)
            });
        }

        private void DrawParticles(Vector3 center, float fadeAlpha)
        {
            foreach (var p in particles)
            {
                Vector3 offset = Quaternion.Euler(0, p.angle, 0) * Vector3.forward * p.distance;
                Vector3 pos = center + offset;
                pos.y = center.y;

                float size = Mathf.Lerp(p.sizeStart, p.sizeEnd, p.Progress);
                float alpha = (1f - p.Progress) * fadeAlpha;

                Material mat = MaterialPool.MatFrom("Other/bulletBa000", ShaderDatabase.MoteGlow);
                mat.color = new Color(1f, 1f, 1f, alpha);
                mat.renderQueue = 4000;

                Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size);
                Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
            }
        }

        private void ApplySunDamage()
        {
            if (Map == null) return;

            int radius = Mathf.CeilToInt(SunRadius * 0.5f);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(centerCell, radius, true))
            {
                if (!cell.InBounds(Map)) continue;

                float dist = cell.DistanceTo(centerCell);
                if (dist > SunRadius * 0.5f) continue;

                List<Thing> thingList = cell.GetThingList(Map);

                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing thing = thingList[i];
                    if (thing == this) continue;

                    if (thing is Pawn pawn && pawn.Spawned && !pawn.Dead)
                    {
                        DamageInfo dinfo = new DamageInfo(
                            DamageDefOf.Burn,
                            20f,
                            0.6f,
                            -1f,
                            this
                        );
                        pawn.TakeDamage(dinfo);
                        continue;
                    }

                    if (thing.def.useHitPoints && thing.Spawned)
                    {
                        if (thing.FlammableNow)
                        {
                            int hp = thing.HitPoints;
                            thing.TakeDamage(new DamageInfo(
                                DamageDefOf.Burn,
                                hp + 10,
                                999f,
                                -1f,
                                this
                            ));

                            if (!cell.GetThingList(Map).Any(t => t.def == ThingDefOf.Filth_Ash))
                            {
                                FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash);
                            }
                        }
                        else
                        {
                            thing.TakeDamage(new DamageInfo(
                                DamageDefOf.Burn,
                                50f,
                                2f,
                                -1f,
                                this
                            ));
                        }
                    }
                }
            }
        }

        private class SunParticle
        {
            public float angle;
            public float distance;
            public int life;

            public float sizeStart;
            public float sizeEnd;

            private const int MaxLife = 40;
            private const float Speed = 0.12f;

            public void Tick()
            {
                life++;
                distance += Speed;
            }

            public float Progress => Mathf.Clamp01((float)life / MaxLife);
            public bool Dead => life >= MaxLife;
        }
    }
}