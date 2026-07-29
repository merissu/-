using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class MarisaShotAGraphics
    {
        public static readonly Material[] Frames = new Material[30];

        static MarisaShotAGraphics()
        {
            for (int i = 0; i < 30; i++)
            {
                Frames[i] = MaterialPool.MatFrom(
                    $"Projectiles/MARISA/shotA/bulletAa{i:D3}",
                    ShaderDatabase.MoteGlow
                );
            }
        }
    }

    public class Projectile_MarisaShotA : Projectile
    {
        private int ticksAlive = 0;
        private const int TicksPerFrame = 2;
        private const float DrawScale = 1.0f;
        public static float NextAngleOffset = 0f;
        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero;

        public override Vector3 ExactPosition => currentRealPos;
        public override Vector3 DrawPos => currentRealPos;
        public override Quaternion ExactRotation => Quaternion.LookRotation(currentVelocity);

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
                    LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false,
                    Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            currentRealPos = origin;

            Vector3 baseDir = (intendedTarget.CenterVector3 - origin).normalized;

            if (NextAngleOffset != 0f)
            {
                baseDir = Quaternion.AngleAxis(NextAngleOffset, Vector3.up) * baseDir;
            }

            currentVelocity = baseDir.normalized;
        }

        protected override void Tick()
        {
            ticksAlive++;

            if (currentRealPos == Vector3.zero)
                currentRealPos = this.DrawPos;

            Thing targetThing = this.intendedTarget.Thing;
            bool targetValid = targetThing != null && !targetThing.Destroyed &&
                               !(targetThing is Pawn p && p.Dead);

            bool canTrack = ticksAlive <= 120;

            if (canTrack && targetValid)
            {
                Vector3 targetVector = targetThing.DrawPos;
                Vector3 desiredDir = (targetVector - currentRealPos).normalized;
                float turnRate = 0.05f;
                currentVelocity = Vector3.Slerp(currentVelocity, desiredDir, turnRate).normalized;
            }

            float step = this.def.projectile.speed / 100f;
            currentRealPos += currentVelocity * step;
            this.Position = currentRealPos.ToIntVec3();

            if (targetValid)
            {
                float contactDist = 0.3f;
                if (targetThing is Pawn targetPawn)
                    contactDist += Mathf.Min(targetPawn.RaceProps.baseBodySize * 0.15f, 0.4f);
                else if (targetThing.def != null)
                    contactDist += Mathf.Max(targetThing.def.size.x, targetThing.def.size.z) * 0.5f;

                if (Vector3.Distance(currentRealPos, targetThing.DrawPos) < contactDist)
                {
                    this.Impact(targetThing);
                    return;
                }
            }

            if (!this.Position.InBounds(this.Map))
            {
                this.Destroy();
                return;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = (ticksAlive / TicksPerFrame) % 30;
            Material mat = MarisaShotAGraphics.Frames[frame];

            Vector3 renderPos = currentRealPos;
            renderPos.y = AltitudeLayer.Projectile.AltitudeFor();

            float rotAngle = currentVelocity.AngleFlat() - 90f;
            Quaternion rotation = Quaternion.AngleAxis(rotAngle, Vector3.up);

            Matrix4x4 matrix = Matrix4x4.TRS(
                renderPos,
                rotation,
                new Vector3(DrawScale, 1f, DrawScale)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            Vector3 exactPos = this.ExactPosition;

            if (hitThing != null)
            {
                float damageAmount = this.def.projectile.GetDamageAmount(this.launcher, null);
                float armorPenetration = this.def.projectile.GetArmorPenetration(this.launcher, null);
                DamageDef damageDef = this.def.projectile.damageDef ?? DamageDefOf.Scratch;
                DamageInfo dinfo = new DamageInfo(damageDef, damageAmount, armorPenetration,
                    this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo);
            }

            base.Impact(hitThing, blockedByShield);

            if (map == null) return;

            SoundDef.Named("marisashotB")?.PlayOneShot(new TargetInfo(this.Position, map));

            Thing burstA = ThingMaker.MakeThing(ThingDef.Named("MarisaShotA_BurstA"));
            GenSpawn.Spawn(burstA, exactPos.ToIntVec3(), map);
            if (burstA is Thing_MarisaShotABurstA a)
                a.exactPosition = exactPos;

            int particleCount = Rand.Range(8, 13);
            for (int i = 0; i < particleCount; i++)
            {
                Thing splash = ThingMaker.MakeThing(ThingDef.Named("MarisaShotA_Splash"));
                GenSpawn.Spawn(splash, exactPos.ToIntVec3(), map);
                if (splash is Thing_MarisaShotASplash sp)
                {
                    sp.exactPosition = exactPos;

                    float angle = Rand.Range(0f, 360f);
                    float speed = Rand.Range(0.15f, 0.35f);
                    Vector3 dir = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad));
                    sp.velocity = dir * speed;
                    sp.startScale = Rand.Range(0.3f, 0.6f);
                    sp.rotation = Rand.Range(0f, 360f);
                }
            }
        }
    }

    public abstract class HakkeroAttackMode
    {
        public abstract string ModeName { get; }
        protected abstract string ProjectileDefName { get; }
        protected abstract string SoundDefName { get; }
        public abstract int BurstCount { get; }
        public abstract int TicksBetweenShots { get; }
        public abstract float WarmupTime { get; }
        public virtual bool PlaySoundOnEveryShot => true;

        public virtual ThingDef ProjectileDef => ThingDef.Named(ProjectileDefName);
        public virtual SoundDef CastSound => SoundDef.Named(SoundDefName);
        public virtual float GetAngleOffsetForShot(int burstShotsLeft) => 0f;
        public virtual void OnWarmupStart(Verb_HakkeroRandomShoot verb, LocalTargetInfo target) { }
        public virtual bool OverrideCastShot(Verb_HakkeroRandomShoot verb, LocalTargetInfo target) => false;
        public virtual void OnCastShot(Verb_HakkeroRandomShoot verb, LocalTargetInfo target) { }
    }

    public class AttackMode_HakkeroBurst5 : HakkeroAttackMode
    {
        public override string ModeName => "HakkeroBurst5";
        protected override string ProjectileDefName => "Bullet_MarisaShotA";
        protected override string SoundDefName => "marisashotA";

        public override int BurstCount => 5;
        public override int TicksBetweenShots => 5;
        public override float WarmupTime => 0.7f;

        public override float GetAngleOffsetForShot(int burstShotsLeft)
        {
            return (burstShotsLeft - 3) * 5f;
        }
    }

    public class Thing_MarisaShotABurstA : Thing
    {
        private int age = 0;
        private const int MaxAge = 10;
        public Vector3 exactPosition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (exactPosition == Vector3.zero) exactPosition = this.Position.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float scale = Mathf.Lerp(0.5f, 2f, progress);
            float alpha = 1f - progress;

            Material mat = FadedMaterialPool.FadedVersionOf(
                MaterialPool.MatFrom("Projectiles/MARISA/shotA/bulletAb003", ShaderDatabase.MoteGlow),
                alpha
            );

            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Quaternion rot = Quaternion.identity;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc,
                rot,
                new Vector3(scale, 1f, scale)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_MarisaShotASplash : Thing
    {
        private int age = 0;
        private const int MaxAge = 30;
        public Vector3 exactPosition;
        public Vector3 velocity;
        public float startScale = 0.5f;
        public float rotation = 0f;

        private const float Deceleration = 0.92f;

        public override Vector3 DrawPos => exactPosition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (exactPosition == Vector3.zero)
                exactPosition = this.Position.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            exactPosition += velocity;
            velocity *= Deceleration;

            this.Position = exactPosition.ToIntVec3();

            if (age >= MaxAge)
                this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float scale = Mathf.Lerp(startScale, 0f, progress);
            float alpha = 1f - progress;

            Material mat = FadedMaterialPool.FadedVersionOf(
                MaterialPool.MatFrom("Projectiles/bulletDa002", ShaderDatabase.MoteGlow),
                alpha
            );

            Vector3 renderPos = DrawPos;
            renderPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Quaternion rot = Quaternion.AngleAxis(rotation, Vector3.up);

            Matrix4x4 matrix = Matrix4x4.TRS(
                renderPos,
                rot,
                new Vector3(scale, 1f, scale)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}