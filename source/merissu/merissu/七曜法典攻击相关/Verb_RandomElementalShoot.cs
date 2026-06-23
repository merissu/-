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
        public virtual void OnWarmupStart(Verb_RandomElementalShoot verb, LocalTargetInfo target) { }
        public virtual bool PlaySoundOnEveryShot => true;

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



    public class Verb_RandomElementalShoot : Verb_Shoot
    {
        private static List<GrimoireAttackMode> availableModes;

        private static FieldInfo cachedBurstField = typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo cachedTicksField = typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.NonPublic | BindingFlags.Instance);

        private GrimoireAttackMode currentMode;
        private bool isVerbPropsCloned = false;

        private List<GrimoireAttackMode> drawPile = new List<GrimoireAttackMode>();

        private void InitializeModesIfNeed()
        {
            if (availableModes == null)
            {
                availableModes = new List<GrimoireAttackMode>
                {
                    new AttackMode_Waterbullet(),
                    new AttackMode_Fireball(),
                    new AttackMode_WaterJade(),
                    new AttackMode_GiantFireball(),
                    new AttackMode_FireMistSpray(),
                    new AttackMode_WindBullet(),
                    new AttackMode_AutumnEdge(),
                    new AttackMode_AutumnBlade(),
                    new AttackMode_FallSlasher(),
                    new AttackMode_DoyouSpear()
                };//攻击在这里new
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

            if (availableModes.Count > 0)
            {
                if (drawPile.Count == 0)
                {
                    drawPile.AddRange(availableModes);

                    for (int i = 0; i < drawPile.Count; i++)
                    {
                        int swapIndex = Rand.Range(i, drawPile.Count);
                        var temp = drawPile[i];
                        drawPile[i] = drawPile[swapIndex];
                        drawPile[swapIndex] = temp;
                    }

                    if (drawPile.Count > 1 && currentMode != null && drawPile[0] == currentMode)
                    {
                        var temp = drawPile[0];
                        drawPile[0] = drawPile[1];
                        drawPile[1] = temp;
                    }
                }

                currentMode = drawPile[0];
                drawPile.RemoveAt(0);
            }

            if (currentMode != null)
            {
                verbProps.warmupTime = currentMode.WarmupTime;
                verbProps.burstShotCount = currentMode.BurstCount;
                verbProps.ticksBetweenBurstShots = currentMode.TicksBetweenShots;

                verbProps.soundCast = currentMode.PlaySoundOnEveryShot ? currentMode.CastSound : null;

                cachedBurstField?.SetValue(this, null);
                cachedTicksField?.SetValue(this, null);
                currentMode.OnWarmupStart(this, castTarg);
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