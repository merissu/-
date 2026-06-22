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
    public static class AutumnEdgeGraphics
    {
        public static readonly Material BulletMat = MaterialPool.MatFrom("Projectiles/AUTUMNEDGE/bulletMa000", ShaderDatabase.Mote);
        public static readonly Material GlowMat = MaterialPool.MatFrom("Projectiles/AUTUMNEDGE/bulletMb000", ShaderDatabase.MoteGlow);
    }

    public class AttackMode_AutumnEdge : GrimoireAttackMode
    {
        public override string ModeName => "AutumnEdge";
        protected override string ProjectileDefName => "Projectile_AutumnEdge";
        protected override string SoundDefName => "AUTUMNEDGE";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 0.8f;

        private Thing_AutumnEdgeCharge currentCharge;

        public override void OnWarmupStart(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return;

            currentCharge = (Thing_AutumnEdgeCharge)ThingMaker.MakeThing(ThingDef.Named("Mote_AutumnEdgeCharge"));
            currentCharge.Setup(caster, target, ProjectileDef);
            GenSpawn.Spawn(currentCharge, caster.Position, map);
        }

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            if (currentCharge != null && !currentCharge.Destroyed)
            {
                currentCharge.FireAll();
            }
            return true;
        }
    }

    public class Thing_AutumnEdgeCharge : Thing
    {
        private Pawn caster;
        private LocalTargetInfo target;
        private ThingDef projDef;
        private int ticksAge = 0;

        private class ChargeBullet
        {
            public Vector3 offset;
            public Vector3 direction;
            public int spawnTick;
        }
        private List<ChargeBullet> bullets = new List<ChargeBullet>();

        private const int SpawnInterval = 4;
        private const float ArcRadius = 2f;

        public Thing_AutumnEdgeCharge() { }

        public void Setup(Pawn caster, LocalTargetInfo target, ThingDef projDef)
        {
            this.caster = caster;
            this.target = target;
            this.projDef = projDef;
        }

        protected override void Tick()
        {
            if (caster == null || caster.Dead || caster.Downed || !(caster.stances.curStance is Stance_Warmup))
            {
                this.Destroy();
                return;
            }

            this.Position = caster.Position;
            ticksAge++;

            Vector3 baseForward = (target.Cell.ToVector3Shifted() - caster.DrawPos).normalized;

            if (ticksAge == 1)
            {
                bullets.Add(new ChargeBullet { offset = baseForward * 1.5f, direction = baseForward, spawnTick = ticksAge });
                PlayChargeSound();
            }
            else if (ticksAge % SpawnInterval == 0)
            {
                int pairIndex = ticksAge / SpawnInterval;
                float angle = pairIndex * 10f;

                Vector3 leftDir = Quaternion.Euler(0, -angle, 0) * baseForward;
                Vector3 rightDir = Quaternion.Euler(0, angle, 0) * baseForward;

                bullets.Add(new ChargeBullet { offset = leftDir * ArcRadius, direction = leftDir, spawnTick = ticksAge });
                bullets.Add(new ChargeBullet { offset = rightDir * ArcRadius, direction = rightDir, spawnTick = ticksAge });
                PlayChargeSound();
            }
        }

        private void PlayChargeSound()
        {
            SoundDef.Named("AUTUMNEDGEcharging")?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
        }

        public void FireAll()
        {
            if (this.Destroyed) return;
            Map map = this.Map;

            List<Projectile_AutumnEdge> spawnedProjectiles = new List<Projectile_AutumnEdge>();

            foreach (var b in bullets)
            {
                Vector3 finalPos = caster.DrawPos + b.offset;
                Projectile_AutumnEdge proj = (Projectile_AutumnEdge)GenSpawn.Spawn(projDef, finalPos.ToIntVec3(), map);

                IntVec3 fakeTargetPos = (finalPos + b.direction * 60f).ToIntVec3();
                fakeTargetPos.x = Mathf.Clamp(fakeTargetPos.x, 0, map.Size.x - 1);
                fakeTargetPos.z = Mathf.Clamp(fakeTargetPos.z, 0, map.Size.z - 1);

                proj.Launch(caster, finalPos, new LocalTargetInfo(fakeTargetPos), target, ProjectileHitFlags.All, false, null);

                spawnedProjectiles.Add(proj);

                Thing_AutumnEdgeEffectFade fadeMote = (Thing_AutumnEdgeEffectFade)ThingMaker.MakeThing(ThingDef.Named("Mote_AutumnEdgeGlowFade"));
                GenSpawn.Spawn(fadeMote, finalPos.ToIntVec3(), map);
                fadeMote.exactPosition = finalPos;
                fadeMote.exactRotationY = Quaternion.LookRotation(b.direction).eulerAngles.y;
            }

            foreach (var p in spawnedProjectiles)
            {
                p.siblings = spawnedProjectiles;
            }

            this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            foreach (var b in bullets)
            {
                Vector3 worldPos = caster.DrawPos + b.offset;
                worldPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                float lifeProgress = Mathf.Clamp01((ticksAge - b.spawnTick) / 10f);
                float scaleX = Mathf.Lerp(3.0f, 1f, lifeProgress);
                float scaleZ = Mathf.Lerp(0.1f, 1f, lifeProgress);

                Quaternion rot = Quaternion.LookRotation(b.direction);

                Matrix4x4 glowMatrix = Matrix4x4.TRS(worldPos, rot, new Vector3(scaleX * 1.2f, 1f, scaleZ * 1.2f));
                Graphics.DrawMesh(MeshPool.plane10, glowMatrix, AutumnEdgeGraphics.GlowMat, 0);

                Matrix4x4 bulletMatrix = Matrix4x4.TRS(worldPos + new Vector3(0, 0.01f, 0), rot, new Vector3(scaleX, 1f, scaleZ));
                Graphics.DrawMesh(MeshPool.plane10, bulletMatrix, AutumnEdgeGraphics.BulletMat, 0);
            }
        }
    }

    public class Projectile_AutumnEdge : Bullet
    {
        public bool isBounce = false;
        public List<Projectile_AutumnEdge> siblings = new List<Projectile_AutumnEdge>();
        public List<Pawn> hitTargets = new List<Pawn>();

        private float cachedHitRadius = -1f;
        protected float BulletHitRadius
        {
            get
            {
                if (cachedHitRadius < 0f)
                {
                    float drawSizeX = this.def.graphicData?.drawSize.x ?? 1f;
                    float visualRadius = drawSizeX / 2f;
                    cachedHitRadius = visualRadius * 0.3333f;
                }
                return cachedHitRadius;
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (this.Destroyed) return;

            if (this.Spawned && Find.TickManager.TicksGame % 1 == 0)
            {
                Thing_AutumnEdgeEffectShrink trail = (Thing_AutumnEdgeEffectShrink)ThingMaker.MakeThing(ThingDef.Named("Mote_AutumnEdgeTrail"));
                GenSpawn.Spawn(trail, this.Position, this.Map);
                trail.exactPosition = this.ExactPosition;
                trail.exactRotationY = this.ExactRotation.eulerAngles.y;
            }

            CheckAdvancedCollision();
        }

        private void CheckAdvancedCollision()
        {
            Vector3 exactPos = this.ExactPosition;
            IntVec3 intPos = exactPos.ToIntVec3();

            if (!intPos.InBounds(Map)) return;

            float searchRadius = Mathf.Max(1.2f, this.def.graphicData?.drawSize.x ?? 1f);
            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(intPos, Map, searchRadius, true);

            foreach (Thing thing in list)
            {
                if (thing == launcher) continue;

                if (thing is Pawn p && !p.Dead && !p.Downed && p.HostileTo(launcher?.Faction))
                {
                    Vector3 targetPos = p.DrawPos;
                    targetPos.y = exactPos.y;

                    float distance = Vector3.Distance(exactPos, targetPos);
                    float targetHitRadius = 0.4f; 
                    if (distance <= (this.BulletHitRadius + targetHitRadius))
                    {
                        this.Impact(p);
                        return;
                    }
                }
                else if (thing is Building b && b.def.fillPercent > 0)
                {
                    if (b is Building_Turret && launcher != null && b.Faction == launcher.Faction) continue;

                    if (b.OccupiedRect().Contains(intPos))
                    {
                        this.Impact(b);
                        return;
                    }
                }
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            Vector3 currentPos = this.DrawPos;
            Pawn launcherPawn = launcher as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (isBounce) return;

            if (hitThing is Pawn p)
            {
                hitTargets.Add(p);

                Pawn targetToLockOn = FindNextTarget(map, currentPos, launcherPawn);

                if (targetToLockOn != null)
                {
                    if (siblings != null)
                    {
                        foreach (var sib in siblings)
                        {
                            if (sib != this && !sib.Destroyed)
                            {
                                Vector3 sibPos = sib.DrawPos;
                                sib.Destroy();
                                SpawnNextBounce(targetToLockOn, map, sibPos, launcherPawn);
                            }
                        }
                    }
                    SpawnNextBounce(targetToLockOn, map, currentPos, launcherPawn);
                }
                else
                {
                    if (siblings != null)
                    {
                        foreach (var sib in siblings)
                        {
                            if (sib != this && !sib.Destroyed)
                            {
                                sib.Destroy();
                            }
                        }
                    }
                }
            }
        }

        private Pawn FindNextTarget(Map map, Vector3 origin, Pawn launcherPawn)
        {
            return map.mapPawns.AllPawnsSpawned
                .Where(target => target.HostileTo(launcherPawn) && !target.Downed && !target.Dead)
                .Where(target => !hitTargets.Contains(target))
                .Where(target => target.Position.DistanceTo(origin.ToIntVec3()) <= 30f)
                .Where(target => GenSight.LineOfSight(origin.ToIntVec3(), target.Position, map))
                .OrderBy(target => target.Position.DistanceToSquared(origin.ToIntVec3()))
                .FirstOrDefault();
        }

        private void SpawnNextBounce(Pawn target, Map map, Vector3 origin, Pawn launcherPawn)
        {
            Projectile_AutumnEdge nextBullet = (Projectile_AutumnEdge)GenSpawn.Spawn(this.def, origin.ToIntVec3(), map);

            nextBullet.launcher = launcherPawn;
            nextBullet.isBounce = true;
            nextBullet.hitTargets = new List<Pawn>(this.hitTargets);

            nextBullet.Launch(launcherPawn, origin, target, target, ProjectileHitFlags.All, false, null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isBounce, "isBounce", false);
            Scribe_Collections.Look(ref hitTargets, "hitTargets", LookMode.Reference);
            Scribe_Collections.Look(ref siblings, "siblings", LookMode.Reference);
        }
    }

    public class Thing_AutumnEdgeEffectFade : Thing
    {
        private int age = 0;
        private const int MaxAge = 20;
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
            float scale = Mathf.Lerp(1.2f, 3.5f, progress);
            float alpha = 1f - progress;
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(AutumnEdgeGraphics.GlowMat, alpha);
            Quaternion rot = Quaternion.Euler(0, exactRotationY, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, new Vector3(scale, 1f, scale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_AutumnEdgeEffectShrink : Thing
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
            float scale = Mathf.Lerp(1.2f, 0.1f, progress);
            float alpha = 1f - progress;
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(AutumnEdgeGraphics.GlowMat, alpha);
            Quaternion rot = Quaternion.Euler(0, exactRotationY, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, new Vector3(scale, 1f, scale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}