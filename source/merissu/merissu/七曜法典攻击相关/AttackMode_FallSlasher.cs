using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class FallSlasherGraphics
    {
        public static readonly Material SwordMat = MaterialPool.MatFrom("Projectiles/FallSlasher/bulletHa000", ShaderDatabase.Transparent);
        public static readonly Material ShockwaveMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAg000", ShaderDatabase.Transparent);
        public static readonly Material TrailMat = MaterialPool.MatFrom("Projectiles/FallSlasher/bulletHb000", ShaderDatabase.Transparent);
    }

    public class AttackMode_FallSlasher : GrimoireAttackMode
    {
        public override string ModeName => "FallSlasher";
        protected override string ProjectileDefName => "Projectile_FallSlasher";
        protected override string SoundDefName => ""; 
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 3f; 

        private Thing_FallSlasherCharge currentCharge;

        public override void OnWarmupStart(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return;

            SoundDef.Named("FallSlashercharging")?.PlayOneShot(new TargetInfo(caster.Position, map));

            currentCharge = (Thing_FallSlasherCharge)ThingMaker.MakeThing(ThingDef.Named("Mote_FallSlasherCharge"));
            currentCharge.Setup(caster, target, ProjectileDef);
            GenSpawn.Spawn(currentCharge, caster.Position, map);
        }

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            if (currentCharge != null && !currentCharge.Destroyed)
            {
                currentCharge.FinishWarmup();
            }
            return true; 
        }
    }

    public class Thing_FallSlasherCharge : Thing
    {
        private Pawn caster;
        private LocalTargetInfo initialTarget;
        private ThingDef projDef;
        private int ticksAge = 0;
        private bool warmupFinished = false;
        private int postWarmupTicks = 0;

        private const int MaxTimeoutTicks = 360;
        private const int SpawnInterval = 9;

        private List<Pawn> cachedTargets = new List<Pawn>();

        private class FloatingSword
        {
            public Vector3 targetOffset;
            public Vector3 currentOffset;
            public float targetRotation;
            public float currentRotation;
            public int spawnTick;
            public bool isFired;
            public Pawn lockedTarget;
            public bool isAiming;
        }

        private List<FloatingSword> swords = new List<FloatingSword>();

        public void Setup(Pawn caster, LocalTargetInfo target, ThingDef projDef)
        {
            this.caster = caster;
            this.initialTarget = target;
            this.projDef = projDef;
        }

        public void FinishWarmup()
        {
            warmupFinished = true;
        }

        protected override void Tick()
        {
            if (caster == null || caster.Dead || caster.Downed)
            {
                this.Destroy();
                return;
            }

            this.Position = caster.Position;
            ticksAge++;

            if (!warmupFinished && !(caster.stances.curStance is Stance_Warmup))
            {
                warmupFinished = true; 
            }

            Vector3 aimDirection = (initialTarget.Cell.ToVector3Shifted() - caster.DrawPos).normalized;
            float baseAimAngle = aimDirection.AngleFlat();

            if (!warmupFinished)
            {
                if (ticksAge % SpawnInterval == 0)
                {
                    FloatingSword sword = new FloatingSword();

                    float angleOffset = Rand.Range(90f, 270f);
                    Vector3 offsetDir = Quaternion.Euler(0, angleOffset, 0) * aimDirection;
                    sword.targetOffset = offsetDir * Rand.Range(1.2f, 5.2f);
                    sword.currentOffset = Vector3.zero;

                    sword.targetRotation = baseAimAngle + Rand.Range(-15f, 15f);
                    sword.currentRotation = sword.targetRotation - 360f;

                    sword.spawnTick = ticksAge;
                    swords.Add(sword);
                }
            }
            else
            {
                postWarmupTicks++;
            }

            int unfiredCount = 0;

            if (warmupFinished && postWarmupTicks % 10 == 0)
            {
                UpdateEnemyTargets();
            }

            int targetIndex = 0; 

            foreach (var sword in swords)
            {
                if (sword.isFired) continue;
                unfiredCount++;

                int age = ticksAge - sword.spawnTick;

                if (!sword.isAiming)
                {
                    float progress = Mathf.Clamp01(age / 60f);
                    sword.currentOffset = Vector3.Lerp(sword.currentOffset, sword.targetOffset, progress * 0.1f);
                    sword.currentRotation = Mathf.Lerp(sword.currentRotation, sword.targetRotation, progress * 0.1f);
                }

                if (warmupFinished)
                {
                    if (sword.lockedTarget == null || sword.lockedTarget.Dead || sword.lockedTarget.Downed)
                    {
                        if (cachedTargets.Count > 0)
                        {
                            sword.lockedTarget = cachedTargets[targetIndex % cachedTargets.Count];
                            targetIndex++;
                        }
                        else
                        {
                            sword.lockedTarget = null; 
                        }
                    }

                    if (sword.lockedTarget != null)
                    {
                        sword.isAiming = true;
                        Vector3 toTarget = sword.lockedTarget.DrawPos - (caster.DrawPos + sword.currentOffset);
                        float targetAngle = toTarget.AngleFlat();

                        sword.currentRotation = Mathf.MoveTowardsAngle(sword.currentRotation, targetAngle, 8f);

                        if (Mathf.Abs(Mathf.DeltaAngle(sword.currentRotation, targetAngle)) < 3f || postWarmupTicks > MaxTimeoutTicks)
                        {
                            FireSword(sword, new LocalTargetInfo(sword.lockedTarget));
                        }
                    }
                    else if (postWarmupTicks > MaxTimeoutTicks)
                    {
                        sword.isAiming = true;

                        Vector3 spawnPos = caster.DrawPos + sword.currentOffset;
                        Vector3 forwardDir = Quaternion.Euler(0, sword.targetRotation, 0) * Vector3.forward;
                        Vector3 blindTargetPos = spawnPos + forwardDir * 30f;

                        FireSword(sword, new LocalTargetInfo(blindTargetPos.ToIntVec3()));
                    }
                }
            }

            if (warmupFinished && unfiredCount == 0)
            {
                this.Destroy();
            }
        }

        private void UpdateEnemyTargets()
        {
            float searchRadiusSq = 35f * 35f;
            cachedTargets.Clear();

            var validPawns = this.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(caster.Faction) && !p.Dead && !p.Downed)
                .Where(p => p.Position.DistanceToSquared(caster.Position) <= searchRadiusSq)
                .Where(p => GenSight.LineOfSight(caster.Position, p.Position, this.Map))
                .OrderBy(p => p.Position.DistanceToSquared(caster.Position));

            cachedTargets.AddRange(validPawns);
        }

        private void FireSword(FloatingSword sword, LocalTargetInfo target)
        {
            sword.isFired = true;
            Vector3 spawnPos = caster.DrawPos + sword.currentOffset;

            Projectile proj = (Projectile)GenSpawn.Spawn(
                projDef,
                spawnPos.ToIntVec3(),
                this.Map);

            proj.Launch(
                caster,
                spawnPos,
                target,
                target,
                ProjectileHitFlags.All,
                false,
                null);

            Thing_FallSlasherShockwave shockwave = (Thing_FallSlasherShockwave)ThingMaker.MakeThing(ThingDef.Named("Mote_FallSlasherShockwave"));
            GenSpawn.Spawn(shockwave, spawnPos.ToIntVec3(), this.Map);
            shockwave.exactPosition = spawnPos;
            shockwave.exactRotationY = sword.currentRotation;

            SoundDef.Named("FallSlasher")?.PlayOneShot(new TargetInfo(spawnPos.ToIntVec3(), this.Map));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            foreach (var sword in swords)
            {
                if (sword.isFired) continue;

                Vector3 worldPos = caster.DrawPos + sword.currentOffset;
                worldPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                Quaternion rot = Quaternion.Euler(0, sword.currentRotation, 0);
                Matrix4x4 matrix = Matrix4x4.TRS(
                    worldPos,
                    rot,
                    new Vector3(3f, 1f, 3f)
                );
                Graphics.DrawMesh(MeshPool.plane10, matrix, FallSlasherGraphics.SwordMat, 0);
            }
        }
    }
    public class Projectile_FallSlasher : Bullet
    {
        protected override void Tick()
        {
            base.Tick();
            if (this.Destroyed) return;

            if (this.Spawned && Find.TickManager.TicksGame % 2 == 0)
            {
                Thing_FallSlasherTrail trail = (Thing_FallSlasherTrail)ThingMaker.MakeThing(ThingDef.Named("Mote_FallSlasherTrail"));
                GenSpawn.Spawn(trail, this.Position, this.Map);
                trail.exactPosition = this.ExactPosition;
                trail.exactRotationY = this.ExactRotation.eulerAngles.y;
            }
        }
    }

    public class Thing_FallSlasherShockwave : Thing
    {
        private int age = 0;
        private const int MaxAge = 15; 
        public Vector3 exactPosition;
        public float exactRotationY = 0f;

        protected override void Tick()
        {
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;

            float scaleWidth = Mathf.Lerp(0.5f, 2.5f, progress);

            float scaleLength = scaleWidth * 2f;

            float alpha = 1f - progress;

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.05f;

            Vector3 hiltOffset =
                Quaternion.Euler(0f, exactRotationY, 0f) *
                new Vector3(0f, 0f, -0.6f);

            Material mat =
                FadedMaterialPool.FadedVersionOf(
                    FallSlasherGraphics.ShockwaveMat,
                    alpha);

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos + hiltOffset,
                Quaternion.Euler(0f, exactRotationY, 0f),
                new Vector3(scaleLength, 1f, scaleWidth)
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0);
        }
    }

    public class Thing_FallSlasherTrail : Thing
    {
        private int age = 0;
        private const int MaxAge = 12;
        public Vector3 exactPosition;
        public float exactRotationY = 0f;

        protected override void Tick()
        {
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float alpha = 1f - progress;
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.Projectile.AltitudeFor() - 0.01f; 

            Material mat = FadedMaterialPool.FadedVersionOf(FallSlasherGraphics.TrailMat, alpha);
            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0, exactRotationY, 0),
                new Vector3(3f, 1f, 3f)
            );
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}