using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
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

        public virtual void OnCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
        }
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
        protected override string ProjectileDefName => "Projectile_NoachianDeluge";
        protected override string SoundDefName => "Waterbullet"; 

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 2.5f;    
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
                    new AttackMode_Fireball()
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
            bool shotSuccess = base.TryCastShot();

            if (shotSuccess && currentMode != null)
            {
                currentMode.OnCastShot(this, this.currentTarget);
            }

            return shotSuccess;
        }
    }
}