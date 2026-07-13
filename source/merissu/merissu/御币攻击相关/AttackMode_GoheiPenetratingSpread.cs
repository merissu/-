using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_GoheiPenetratingSpread : GoheiAttackMode
    {
        public override string ModeName => "GoheiPenetratingSpread";
        protected override string ProjectileDefName => "REIMU_PenetratingBullet";
        protected override string SoundDefName => "ReimuTalismanA";   
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.2f;

        private const int ProjectileCount = 5;
        private const float SpreadAngle = 10f;  

        public override bool OverrideCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            SoundDef.Named(SoundDefName)?.PlayOneShot(new TargetInfo(caster.Position, map));

            Vector3 aimDir;
            if (target.IsValid && target.Cell != caster.Position)
                aimDir = (target.Cell.ToVector3Shifted() - caster.DrawPos).normalized;
            else
                aimDir = caster.Rotation.FacingCell.ToVector3().normalized;

            float baseAngle = aimDir.AngleFlat();
            float halfSpread = SpreadAngle * (ProjectileCount - 1) / 2f;

            for (int i = 0; i < ProjectileCount; i++)
            {
                float offset = -halfSpread + i * SpreadAngle;
                float finalAngle = baseAngle + offset;
                Vector3 launchDir = Quaternion.Euler(0f, finalAngle, 0f) * Vector3.forward;

                Vector3 farPoint = caster.DrawPos + launchDir * 50f;
                IntVec3 targetCell = farPoint.ToIntVec3();
                if (map != null) targetCell = targetCell.ClampInsideMap(map);

                Projectile projectile = (Projectile)GenSpawn.Spawn(
                    ThingDef.Named("REIMU_PenetratingBullet"), caster.Position, map);
                LocalTargetInfo bulletTarget = new LocalTargetInfo(targetCell);
                projectile.Launch(caster, caster.DrawPos, bulletTarget, bulletTarget,
                    ProjectileHitFlags.IntendedTarget, false, null, null);
            }

            return true;
        }
    }
}