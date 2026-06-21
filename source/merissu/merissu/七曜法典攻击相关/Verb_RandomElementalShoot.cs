using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public abstract class GrimoireAttackMode
    {
        public abstract string ModeName { get; }
        protected abstract string ProjectileDefName { get; }
        protected abstract string SoundDefName { get; }
        public abstract int BurstCount { get; }
        public abstract int TicksBetweenShots { get; }
        public abstract float WarmupTime { get; }

        private ThingDef cachedProjectile;
        public ThingDef ProjectileDef
        {
            get
            {
                if (cachedProjectile == null) cachedProjectile = ThingDef.Named(ProjectileDefName);
                return cachedProjectile;
            }
        }

        private SoundDef cachedSound;
        public SoundDef CastSound
        {
            get
            {
                if (cachedSound == null && !string.IsNullOrEmpty(SoundDefName))
                    cachedSound = SoundDef.Named(SoundDefName);
                return cachedSound;
            }
        }

        public virtual bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            return false;
        }

        public virtual void OnCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target) { }
    }

    public class AttackMode_Waterbullet : GrimoireAttackMode
    {
        public override string ModeName => "Waterbullet";
        protected override string ProjectileDefName => "Projectile_NoachianDeluge";
        protected override string SoundDefName => "Waterbullet"; 

        public override int BurstCount => 10;
        public override int TicksBetweenShots => 2; 
        public override float WarmupTime => 1.2f;    
    }

    public class AttackMode_Fireball : GrimoireAttackMode
    {
        public override string ModeName => "Fireball";
        protected override string ProjectileDefName => "Projectile_Fireball";
        protected override string SoundDefName => "Fireball";

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1f;

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
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            Thing shockwave = ThingMaker.MakeThing(ThingDef.Named("Fireball_Shockwave"));
            GenSpawn.Spawn(shockwave, spawnCell, map);
            if (shockwave is Thing_FireballShockwave sw) sw.exactPosition = spawnPos;

            float baseAngle = caster.Rotation.AsAngle;
            float[] spreadAngles = new float[] { 0f, 72f, 144f, -72f, -144f };

            foreach (float angleOffset in spreadAngles)
            {
                float finalAngle = baseAngle + angleOffset;
                Vector3 projDir = Vector3Utility.FromAngleFlat(finalAngle);

                Vector3 projTargetPos = spawnPos + projDir * 20f;
                LocalTargetInfo projTargetInfo = new LocalTargetInfo(projTargetPos.ToIntVec3());

                Projectile proj = (Projectile)GenSpawn.Spawn(ProjectileDef, spawnCell, map);

                LocalTargetInfo hitTarget = (angleOffset == 0f) ? target : projTargetInfo;

                proj.Launch(caster, spawnPos, projTargetInfo, hitTarget, ProjectileHitFlags.All, false, null, null);
            }

            return true;
        }
    }
    public class AttackMode_WaterJade : GrimoireAttackMode
    {
        public override string ModeName => "WaterJade";
        protected override string ProjectileDefName => "Projectile_WaterJadePiercing";
        protected override string SoundDefName => "WaterJadePiercing"; 

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.5f;

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
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            float baseAngle = dir.AngleFlat() - 90f;
            float[] spreadAngles = new float[] { -45f, -30f, -15f, 0f, 15f, 30f, 45f };

            foreach (float angleOffset in spreadAngles)
            {
                float finalAngle = baseAngle + angleOffset;
                Vector3 projDir = Vector3Utility.FromAngleFlat(finalAngle);

                Vector3 projTargetPos = spawnPos + projDir * 25f;
                LocalTargetInfo projTargetInfo = new LocalTargetInfo(projTargetPos.ToIntVec3());

                Projectile proj = (Projectile)GenSpawn.Spawn(ProjectileDef, spawnCell, map);

                proj.Launch(caster, spawnPos, projTargetInfo, projTargetInfo, ProjectileHitFlags.None, false, null, null);
            }

            return true;
        }
    }
    public class AttackMode_GiantFireball : GrimoireAttackMode
    {
        public override string ModeName => "GiantFireball";
        protected override string ProjectileDefName => "Projectile_GiantFireball";
        protected override string SoundDefName => "BigFireball"; 

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.5f; 

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
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            Thing blastAnim = ThingMaker.MakeThing(ThingDef.Named("Mote_DirectionalFireBlast"));
            GenSpawn.Spawn(blastAnim, spawnCell, map);
            if (blastAnim is Thing_DirectionalFireBlast anim)
            {
                anim.exactPosition = spawnPos;
                anim.exactRotation = dir.AngleFlat();
            }

            Projectile proj = (Projectile)GenSpawn.Spawn(ProjectileDef, spawnCell, map);
            proj.Launch(caster, spawnPos, target, target, ProjectileHitFlags.All, false, null, null);

            return true;
        }
    }

    public class Verb_RandomElementalShoot : Verb_Shoot
    {
        private static List<GrimoireAttackMode> availableModes;

        private static FieldInfo cachedBurstField = typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo cachedTicksField = typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.NonPublic | BindingFlags.Instance);

        private GrimoireAttackMode currentMode;
        private int lastModeIndex = -1;
        private bool isVerbPropsCloned = false;

        private void InitializeModesIfNeed()
        {
            if (availableModes == null)
            {
                availableModes = new List<GrimoireAttackMode>
                {
                    new AttackMode_Waterbullet(),
                    new AttackMode_Fireball(),
                    new AttackMode_WaterJade(),
                    new AttackMode_GiantFireball()
                    //新攻击在这里new
                };
            }
        }

        private void CloneVerbPropsIfNeed()
        {
            if (!isVerbPropsCloned)
            {
                VerbProperties privateProps = new VerbProperties();
                FieldInfo[] fields = typeof(VerbProperties).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    field.SetValue(privateProps, field.GetValue(this.verbProps));
                }
                this.verbProps = privateProps;
                isVerbPropsCloned = true;
            }
        }

        public override ThingDef Projectile
        {
            get
            {
                if (currentMode != null) return currentMode.ProjectileDef;
                return base.Projectile;
            }
        }

        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            InitializeModesIfNeed();
            CloneVerbPropsIfNeed();

            if (availableModes.Count > 1)
            {
                int nextIndex;
                do
                {
                    nextIndex = Rand.Range(0, availableModes.Count);
                }
                while (nextIndex == lastModeIndex);

                lastModeIndex = nextIndex;
                currentMode = availableModes[nextIndex];
            }
            else if (availableModes.Count == 1)
            {
                currentMode = availableModes[0];
            }

            if (currentMode != null)
            {
                verbProps.warmupTime = currentMode.WarmupTime;
                verbProps.soundCast = currentMode.CastSound;
                verbProps.burstShotCount = currentMode.BurstCount;
                verbProps.ticksBetweenBurstShots = currentMode.TicksBetweenShots;

                cachedBurstField?.SetValue(this, null);
                cachedTicksField?.SetValue(this, null);
            }

            return base.TryStartCastOn(
                castTarg,
                destTarg,
                surpriseAttack,
                canHitNonTargetPawns,
                preventFriendlyFire,
                nonInterruptingSelfCast);
        }

        protected override bool TryCastShot()
        {
            if (currentMode != null && currentMode.OverrideCastShot(this, this.currentTarget))
            {
                currentMode.OnCastShot(this, this.currentTarget); 
                return true;
            }

            bool shotSuccess = base.TryCastShot();
            if (shotSuccess && currentMode != null)
            {
                currentMode.OnCastShot(this, this.currentTarget);
            }

            return shotSuccess;
        }
    }
}