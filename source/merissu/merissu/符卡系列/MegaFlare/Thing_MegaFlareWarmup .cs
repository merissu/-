using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Thing_MegaFlareWarmup : Thing
    {
        public Pawn caster;
        public Vector3 direction;

        public int age;

        public const int TicksPerFrame = 3;
        public const int TotalFrames = 15;
        public const float MaxCoreSize = 4.5f;
        public const int WarmupDuration = 120;
        public const int MainPhaseDuration = 200;
        public const int FadeOutDuration = 30;

        public const int DamageInterval = 1; 
        public const int FireRadius = 15; 
        public const int FireDamage = 30; 

        private bool stanceSet = false;
        private bool mainPhaseStarted = false;
        private int mainPhaseAge = 0;
        private bool fadingOut = false;
        private int fadeOutAge = 0;
        private List<FusionParticle> particles = new List<FusionParticle>();

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
                        new Vector3(-0.5f,0,-0.5f),
                        new Vector3( 0.5f,0,-0.5f),
                        new Vector3(-0.5f,0, 0.5f),
                        new Vector3( 0.5f,0, 0.5f)
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

            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed)
            {
                Destroy();
                return;
            }

            if (!stanceSet)
            {
                LocalTargetInfo focus = caster.Position + (direction * 5f).ToIntVec3();
                caster.stances.SetStance(new Stance_MegaFlare(WarmupDuration, focus, null));
                caster.pather.StopDead();
                stanceSet = true;
            }

            caster.Rotation = Rot4.FromAngleFlat(direction.AngleFlat());
            if (caster.CurJob != null && !caster.CurJob.def.neverShowWeapon)
                caster.jobs.EndCurrentJob(JobCondition.InterruptForced, true);

            Position = caster.Position;

            if (!mainPhaseStarted)
            {
                age++;
                if (age % 5 == 0)
                    SpawnParticle();

                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    particles[i].Tick();
                    if (particles[i].Dead)
                        particles.RemoveAt(i);
                }

                if (age > WarmupDuration)
                    StartMainPhase();
            }
            else
            {
                mainPhaseAge++;

                if (mainPhaseAge % 3 == 0)
                    SpawnMainPhaseParticles();

                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    particles[i].TickMainPhase();
                    if (particles[i].Dead)
                        particles.RemoveAt(i);
                }

                if (mainPhaseAge % DamageInterval == 0)
                    ApplyDamageAndBurnEverything();

                if (!fadingOut && mainPhaseAge > MainPhaseDuration)
                {
                    fadingOut = true;
                    fadeOutAge = 0;
                }

                if (fadingOut)
                {
                    fadeOutAge++;
                    if (fadeOutAge >= FadeOutDuration)
                        Destroy();
                }
            }
        }

        private void StartMainPhase()
        {
            mainPhaseStarted = true;
            mainPhaseAge = 0;
            particles.Clear();

            SoundDef.Named("MegaFlare2").PlayOneShot(new TargetInfo(caster.Position, caster.Map));

            LocalTargetInfo focus = caster.Position + (direction * 16f).ToIntVec3();
            caster.stances.SetStance(new Stance_MegaFlare(MainPhaseDuration, focus, null));

            Job waitJob = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, MainPhaseDuration);
            caster.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
        }

        private void SpawnParticle()
        {
            particles.Add(new FusionParticle
            {
                angle = Rand.Range(0f, 360f),
                distance = Rand.Range(1f, 3f),
                life = 0,
                initialSize = 0.6f,
                finalSize = 1.2f
            });
        }

        private void SpawnMainPhaseParticles()
        {
            int count = Rand.Range(2, 4);

            for (int i = 0; i < count; i++)
            {
                float offsetAngle = Rand.Range(-45f, 45f);
                particles.Add(new FusionParticle
                {
                    angle = direction.AngleFlat() + offsetAngle,
                    distance = 0f,
                    life = 0,
                    expanding = true,
                    maxDistance = Rand.Range(10f, 16f),
                    initialSize = Rand.Range(1.2f, 2f),
                    finalSize = Rand.Range(2.4f, 3.2f)
                });
            }
        }

        private float CoreSize => Mathf.Lerp(1f, MaxCoreSize, (float)age / WarmupDuration);

        private void ApplyDamageAndBurnEverything()
        {
            if (caster.Map == null) return;

            IntVec3 center = caster.Position + (direction * 16f).ToIntVec3();

            foreach (IntVec3 c in GenRadial.RadialCellsAround(center, FireRadius, true))
            {
                if (!c.InBounds(caster.Map)) continue;

                List<Thing> things = c.GetThingList(caster.Map).ToList();
                foreach (Thing thing in things)
                {
                    if (thing == caster)
                        continue; 

                    if (thing is Pawn pawn && pawn.Spawned && !pawn.Dead && !pawn.Downed)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, FireDamage, 0.8f, -1f, caster);
                        pawn.TakeDamage(dinfo);
                    }
                    else if (thing.FlammableNow)
                    {
                        thing.Destroy();
                        GenSpawn.Spawn(ThingDefOf.Filth_Ash, c, caster.Map);
                    }
                    else
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, FireDamage, 0.8f, -1f, caster);
                        thing.TakeDamage(dinfo);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 basePos = caster.DrawPos + direction * 1f;
            basePos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.5f;

            if (!mainPhaseStarted)
            {
                DrawCore(basePos);
                DrawRing(basePos);
            }
            else
            {
                DrawMainPhase(basePos);
            }

            DrawParticles(basePos);
        }

        private void DrawCore(Vector3 pos)
        {
            int frame = (age / TicksPerFrame) % TotalFrames;
            Material mat = MaterialPool.MatFrom($"Other/SUN/bulletOa{frame:D3}", ShaderDatabase.MoteGlow);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * CoreSize);
            Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
        }

        private void DrawRing(Vector3 pos)
        {
            float flicker = 0.6f + Mathf.Sin(age * 0.6f) * 0.4f;
            Material mat = MaterialPool.MatFrom("Other/bulletDb000", ShaderDatabase.MoteGlow);
            mat.color = new Color(1f, 1f, 1f, flicker);
            mat.renderQueue = 4000;

            float widthFactor = 0.25f;
            Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, rotation, new Vector3(CoreSize * widthFactor, 1f, CoreSize));
            Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
        }

        private void DrawMainPhase(Vector3 pos)
        {
            float widthFactor = 0.5f;

            float fadeAlpha = 1f;
            if (fadingOut)
                fadeAlpha = 1f - (float)fadeOutAge / FadeOutDuration;

            for (int i = 1; i <= 3; i++)
            {
                float size = i * 2f;
                float flicker = 0.6f + Mathf.Sin(mainPhaseAge * 0.6f) * 0.4f;
                Material mat = MaterialPool.MatFrom("Other/bulletDb000", ShaderDatabase.MoteGlow);
                mat.color = new Color(1f, 1f, 1f, flicker * fadeAlpha);
                mat.renderQueue = 4000;

                Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
                Matrix4x4 matrix = Matrix4x4.TRS(pos + direction.normalized * 0.1f, rotation, new Vector3(size * widthFactor, 1f, size * 2f));
                Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
            }

            Vector3 center = caster.Position.ToVector3() + direction.normalized * 16f;
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.5f;
            int frame = (mainPhaseAge / TicksPerFrame) % TotalFrames;
            Material matSphere = MaterialPool.MatFrom($"Other/SUN/bulletOa{frame:D3}", ShaderDatabase.MoteGlow);
            matSphere.color = new Color(1f, 1f, 1f, fadeAlpha);
            matSphere.renderQueue = 4000;

            float sphereRadius = 30f;
            Matrix4x4 matrixSphere = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(sphereRadius * 2f, 30f, sphereRadius * 2f));
            Graphics.DrawMesh(QuadMesh, matrixSphere, matSphere, 0);
        }

        private void DrawParticles(Vector3 corePos)
        {
            float fadeAlpha = 1f;
            if (fadingOut)
                fadeAlpha = 1f - (float)fadeOutAge / FadeOutDuration;

            foreach (var p in particles)
            {
                Vector3 offset = Quaternion.Euler(0, p.angle, 0) * Vector3.forward * p.CurrentDistance;
                Vector3 pos = corePos + (mainPhaseStarted ? direction.normalized * 0.1f : Vector3.zero) + offset;
                pos.y = corePos.y;

                float size = Mathf.Lerp(p.initialSize, p.finalSize, p.Progress);
                float alpha = (1f - p.Progress) * fadeAlpha;

                Material mat = MaterialPool.MatFrom("Other/bulletBa000", ShaderDatabase.MoteGlow);
                mat.color = new Color(1f, 1f, 1f, alpha);
                mat.renderQueue = 4000;

                Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size);
                Graphics.DrawMesh(QuadMesh, matrix, mat, 0);
            }
        }

        private class FusionParticle
        {
            public float angle;
            public float distance;
            public int life;
            public bool expanding = false;
            public float maxDistance = 8f;
            public float initialSize = 0.6f;
            public float finalSize = 1.2f;

            private const int MaxLife = 40;
            private const float Speed = 0.25f;

            public void Tick()
            {
                life++;
                distance = Mathf.Lerp(distance, 0f, 0.08f);
            }

            public void TickMainPhase()
            {
                life++;
                if (expanding)
                {
                    distance += Speed;
                    if (distance > maxDistance) distance = maxDistance;
                }
            }

            public float CurrentDistance => distance;
            public float Progress => Mathf.Clamp01((float)life / MaxLife);
            public bool Dead => life >= MaxLife;
        }
    }
}
