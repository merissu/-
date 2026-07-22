using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace merissu
{
    public abstract class WaveGunAttackMode
    {
        public abstract string ModeName { get; }
        protected abstract string ProjectileDefName { get; }
        protected abstract string SoundDefName { get; }
        public abstract int BurstCount { get; }
        public abstract int TicksBetweenShots { get; }
        public abstract float WarmupTime { get; }
        public virtual bool PlaySoundOnEveryShot => true;

        public virtual ThingDef ProjectileDef
        {
            get
            {
                ThingDef def = ThingDef.Named(ProjectileDefName);
                if (def == null)
                {
                    Log.Error($"[merissu] 无法找到子弹定义: {ProjectileDefName}");
                }
                return def;
            }
        }

        public virtual SoundDef CastSound => SoundDef.Named(SoundDefName);

        public virtual void OnWarmupStart(Verb_WaveGunRandomShoot verb, LocalTargetInfo target) { }
        public virtual bool OverrideCastShot(Verb_WaveGunRandomShoot verb, LocalTargetInfo target) => false;
        public virtual void OnCastShot(Verb_WaveGunRandomShoot verb, LocalTargetInfo target) { }
        public virtual LocalTargetInfo? GetAdjustedTarget(Verb_WaveGunRandomShoot verb, LocalTargetInfo target, int shotIndex) => null;
    }

    public class Verb_WaveGunRandomShoot : Verb_Shoot
    {
        private static List<WaveGunAttackMode> availableModes;
        private static FieldInfo cachedBurstField = typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo cachedTicksField = typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.NonPublic | BindingFlags.Instance);

        private WaveGunAttackMode currentMode;
        private bool isVerbPropsCloned = false;
        private List<WaveGunAttackMode> drawPile = new List<WaveGunAttackMode>();

        private void InitializeModesIfNeed()
        {
            if (availableModes == null)
            {
                availableModes = new List<WaveGunAttackMode>
                {
                    new AttackMode_WaveGunBurst5(),
                    new AttackMode_WaveGunChargeExplode()//这里添加模式
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
                castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire,
                nonInterruptingSelfCast);
        }

        protected override bool TryCastShot()
        {
            if (currentMode != null && currentMode.OverrideCastShot(this, this.currentTarget))
            {
                currentMode.OnCastShot(this, this.currentTarget);
                return true;
            }

            LocalTargetInfo originalTarget = this.currentTarget;
            LocalTargetInfo? adjustedTarget = null;
            if (currentMode != null)
            {
                adjustedTarget = currentMode.GetAdjustedTarget(this, originalTarget, 0);
            }

            if (adjustedTarget.HasValue)
            {
                this.currentTarget = adjustedTarget.Value;
            }

            bool success = base.TryCastShot();

            if (adjustedTarget.HasValue)
            {
                this.currentTarget = originalTarget;
            }

            if (success && currentMode != null)
            {
                currentMode.OnCastShot(this, originalTarget);
            }

            return success;
        }
    }
}