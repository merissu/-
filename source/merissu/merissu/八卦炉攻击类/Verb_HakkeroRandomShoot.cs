using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Verb_HakkeroRandomShoot : Verb_Shoot
    {
        private static List<HakkeroAttackMode> availableModes;
        private static FieldInfo cachedBurstField = typeof(Verb).GetField(
            "cachedBurstShotCount", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo cachedTicksField = typeof(Verb).GetField(
            "cachedTicksBetweenBurstShots", BindingFlags.NonPublic | BindingFlags.Instance);

        private HakkeroAttackMode currentMode;
        private bool isVerbPropsCloned = false;
        private List<HakkeroAttackMode> drawPile = new List<HakkeroAttackMode>();

        private void InitializeModesIfNeed()
        {
            if (availableModes == null)
            {
                availableModes = new List<HakkeroAttackMode>
                {
                    new AttackMode_HakkeroBurst5()
                    //新攻击这里new
                };
            }
        }

        private void CloneVerbPropsIfNeed()
        {
            if (!isVerbPropsCloned)
            {
                VerbProperties privateProps = new VerbProperties();
                FieldInfo[] fields = typeof(VerbProperties).GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                    drawPile.Shuffle();
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

            if (currentMode != null)
            {
                Projectile_MarisaShotA.NextAngleOffset = currentMode.GetAngleOffsetForShot(this.burstShotsLeft);
            }

            bool success = base.TryCastShot();

            Projectile_MarisaShotA.NextAngleOffset = 0f;

            if (success && currentMode != null)
            {
                currentMode.OnCastShot(this, this.currentTarget);
            }

            return success;
        }
    }
}