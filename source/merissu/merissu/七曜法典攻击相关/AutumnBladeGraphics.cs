using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class AutumnBladeGraphics
    {
        public static readonly Material BladeMat = MaterialPool.MatFrom("Projectiles/AutumnBlades/spellBulletBa000", ShaderDatabase.Transparent);
        public static readonly Material BladeUnderMat = MaterialPool.MatFrom("Projectiles/AutumnBlades/spellBulletBb000", ShaderDatabase.Transparent);
        public static readonly Material BladeTrailMat = MaterialPool.MatFrom("Projectiles/AutumnBlades/spellBulletBc000", ShaderDatabase.Transparent);
    }

    public class AttackMode_AutumnBlade : GrimoireAttackMode
    {
        public override string ModeName => "AutumnBlade";
        protected override string ProjectileDefName => "Projectile_AutumnBlade";
        protected override string SoundDefName => "AUTUMNEDGE"; 

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.0f;

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            Vector3 casterPos = caster.DrawPos;
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            if (target.Thing != null) targetPos = target.Thing.DrawPos;

            Vector3 dir = (targetPos - casterPos).normalized;
            Vector3 spawnPos = casterPos + dir * 1f;

            Projectile_AutumnBlade proj = (Projectile_AutumnBlade)GenSpawn.Spawn(ProjectileDef, spawnPos.ToIntVec3(), map);
            LocalTargetInfo farTarget = new LocalTargetInfo((spawnPos + dir * 999f).ToIntVec3());
            proj.Launch(caster, spawnPos, farTarget, farTarget, ProjectileHitFlags.All, false, null);

            return true;
        }
    }

    public class Projectile_AutumnBlade : Projectile
    {
        private Vector3 exactPosition;
        private Vector3 velocity;

        private float angleMain = 0f;
        private float angleUnder = 0f;

        private bool isSlowed = false;
        private int slowDurationTicks = 0;
        private const int MaxSlowTicks = 180; 
        private int lastDamageTick = 0;

        public override Vector3 ExactPosition => exactPosition;
        public override Quaternion ExactRotation => Quaternion.LookRotation(velocity);

        public override void Launch(
            Thing launcher,
            Vector3 origin,
            LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags,
            bool preventFriendlyFire = false,
            Thing equipment = null,
            ThingDef targetCoverDef = null)
        {
            base.Launch(
                launcher,
                origin,
                usedTarget,
                intendedTarget,
                hitFlags,
                preventFriendlyFire,
                equipment,
                targetCoverDef);

            exactPosition = origin;

            Vector3 dir = usedTarget.Cell.ToVector3Shifted() - origin;
            dir.y = 0f;

            if (dir.magnitude < 0.1f)
                dir = Vector3.forward;

            velocity = dir.normalized * (def.projectile.speed / 60f);
        }
        protected override void Tick()
        {
            if (this.Destroyed) return;

            angleMain += 35f;
            angleUnder += 60f;

            float speedMultiplier = isSlowed ? 0.1f : 1f; 
            exactPosition += velocity * speedMultiplier;
            this.Position = exactPosition.ToIntVec3();

            if (!this.Position.InBounds(this.Map))
            {
                this.Destroy();
                return;
            }

            if (!isSlowed && Find.TickManager.TicksGame % 5 == 0)
            {
                Thing_AutumnBladeTrail trail = (Thing_AutumnBladeTrail)ThingMaker.MakeThing(ThingDef.Named("Mote_AutumnBladeTrail"));
                GenSpawn.Spawn(trail, this.Position, this.Map);
                trail.exactPosition = this.exactPosition;
            }
            if (isSlowed)
            {
                slowDurationTicks++;
                if (slowDurationTicks >= MaxSlowTicks)
                {
                    ShrinkAndDestroy();
                    return;
                }
            }

            CheckCollisions();
        }

        private void CheckCollisions()
        {
            bool hitEnemyThisTick = false;

            CellRect rect = CellRect.CenteredOn(this.Position, 1);

            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(Map))
                    continue;

                if (cell.Impassable(Map))
                {
                    ShrinkAndDestroy();
                    return;
                }

                List<Thing> things = cell.GetThingList(Map);

                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];

                    if (t == this || t == launcher)
                        continue;

                    Projectile otherProj = t as Projectile;
                    if (otherProj != null && otherProj.launcher != launcher)
                    {
                        otherProj.Destroy();
                        continue;
                    }

                    Pawn p = t as Pawn;
                    if (p != null &&
                        !p.Dead &&
                        p.HostileTo(launcher != null ? launcher.Faction : null))
                    {
                        hitEnemyThisTick = true;

                        if (Find.TickManager.TicksGame - lastDamageTick >= 5)
                        {
                            p.TakeDamage(
                                new DamageInfo(
                                    def.projectile.damageDef,
                                    DamageAmount,
                                    ArmorPenetration,
                                    ExactRotation.eulerAngles.y,
                                    launcher));
                        }
                    }
                }
            }

            if (hitEnemyThisTick &&
                Find.TickManager.TicksGame - lastDamageTick >= 5)
            {
                lastDamageTick = Find.TickManager.TicksGame;
            }

            if (hitEnemyThisTick && !isSlowed)
            {
                isSlowed = true;
            }
        }
        private void ShrinkAndDestroy()
        {
            Thing_AutumnBladeShrink shrink = (Thing_AutumnBladeShrink)ThingMaker.MakeThing(ThingDef.Named("Mote_AutumnBladeShrink"));
            GenSpawn.Spawn(shrink, this.Position, this.Map);
            shrink.exactPosition = this.exactPosition;
            this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.Projectile.AltitudeFor();

            Vector3 underPos = pos;
            underPos.y = AltitudeLayer.Projectile.AltitudeFor() - 0.08f;
            float sizeX = this.def.graphicData.drawSize.x;
            float sizeZ = this.def.graphicData.drawSize.y;

            Matrix4x4 matrixMain = Matrix4x4.TRS(pos, Quaternion.Euler(0, angleMain, 0), new Vector3(sizeX, 1f, sizeZ));
            Graphics.DrawMesh(MeshPool.plane10, matrixMain, AutumnBladeGraphics.BladeMat, 0);

            Matrix4x4 matrixUnder = Matrix4x4.TRS(underPos, Quaternion.Euler(0, -angleUnder, 0), new Vector3(sizeX * 1.5f, 1f, sizeZ * 1.5f));
            Graphics.DrawMesh(MeshPool.plane10, matrixUnder, AutumnBladeGraphics.BladeUnderMat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref velocity, "velocity");
            Scribe_Values.Look(ref isSlowed, "isSlowed", false);
            Scribe_Values.Look(ref slowDurationTicks, "slowDurationTicks", 0);
            Scribe_Values.Look(ref lastDamageTick, "lastDamageTick", 0);
        }
    }

    public class Thing_AutumnBladeTrail : Thing
    {
        public Vector3 exactPosition;
        private float angle = 0f;
        private int age = 0;
        private const int MaxAge = 30;

        protected override void Tick()
        {
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float scale = Mathf.Lerp(3f, 6f, progress);
            float alpha = 1f - progress;

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.Projectile.AltitudeFor() - 0.15f;
            Material mat = FadedMaterialPool.FadedVersionOf(AutumnBladeGraphics.BladeTrailMat, alpha);
            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_AutumnBladeShrink : Thing
    {
        public Vector3 exactPosition;
        private float angle = 0f;
        private int age = 0;
        private const int MaxAge = 10;

        protected override void Tick()
        {
            age++;
            angle += 25f; 
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;

            float scale = Mathf.Lerp(1.5f, 0f, progress);

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.Projectile.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, angle, 0), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, AutumnBladeGraphics.BladeMat, 0);
        }
    }
}