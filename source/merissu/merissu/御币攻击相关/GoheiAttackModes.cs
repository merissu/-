using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace merissu
{
    public abstract class GoheiAttackMode
    {
        public abstract string ModeName { get; }
        protected abstract string ProjectileDefName { get; }
        protected abstract string SoundDefName { get; }
        public abstract int BurstCount { get; }
        public abstract int TicksBetweenShots { get; }
        public abstract float WarmupTime { get; }
        public virtual void OnWarmupStart(Verb_GoheiRandomShoot verb, LocalTargetInfo target) { }
        public virtual bool PlaySoundOnEveryShot => true;

        public virtual LocalTargetInfo? GetAdjustedTarget(Verb_GoheiRandomShoot verb, LocalTargetInfo originalTarget, int shotIndex)
        {
            return null;
        }

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

        public virtual bool OverrideCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            return false; 
        }

        public virtual void OnCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target) { }
    }

    public class AttackMode_GoheiSpread : GoheiAttackMode
    {
        public override string ModeName => "GoheiSpread";
        protected override string ProjectileDefName => "HakureiTalisman";
        protected override string SoundDefName => "gohei";
        public override int BurstCount => 10;
        public override int TicksBetweenShots => 3;    
        public override float WarmupTime => 0.5f;
        public override bool PlaySoundOnEveryShot => true;

        private const float SpreadAngle = 25f;
        private const float AimDistance = 30f;

        public override LocalTargetInfo? GetAdjustedTarget(Verb_GoheiRandomShoot verb, LocalTargetInfo originalTarget, int shotIndex)
        {
            Pawn caster = verb.CasterPawn;
            if (caster == null) return originalTarget;

            Vector3 aimDirection;
            if (originalTarget.IsValid && originalTarget.Cell != caster.Position)
            {
                aimDirection = (originalTarget.Cell.ToVector3Shifted() - caster.DrawPos).normalized;
            }
            else
            {
                aimDirection = caster.Rotation.FacingCell.ToVector3().normalized;
            }

            float baseAngle = aimDirection.AngleFlat();
            float randomOffset = Rand.Range(-SpreadAngle, SpreadAngle);
            float finalAngle = baseAngle + randomOffset;
            Vector3 launchDir = Quaternion.Euler(0f, finalAngle, 0f) * Vector3.forward;

            IntVec3 aimCell = caster.Position + (launchDir * AimDistance).ToIntVec3();
            Map map = caster.Map;
            if (map != null)
            {
                aimCell = aimCell.ClampInsideMap(map);
            }

            return new LocalTargetInfo(aimCell);
        }
    }
}