using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace merissu
{
    public class Verb_GoheiRandomShoot : Verb_Shoot
    {
        private static List<GoheiAttackMode> availableModes;
        private static FieldInfo cachedBurstField = typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo cachedTicksField = typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.NonPublic | BindingFlags.Instance);

        private GoheiAttackMode currentMode;
        private bool isVerbPropsCloned = false;
        private List<GoheiAttackMode> drawPile = new List<GoheiAttackMode>();

        private void InitializeModesIfNeed()
        {
            if (availableModes == null)
            {
                availableModes = new List<GoheiAttackMode>
            {
                    new AttackMode_GoheiSpread(),
                    new AttackMode_GoheiDelayedOrbit(),
                    new AttackMode_GoheiDelayedOrbitSpread(),
                    new AttackMode_GoheiPenetratingSpread(),
                    new AttackMode_VigilanceFormation()
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