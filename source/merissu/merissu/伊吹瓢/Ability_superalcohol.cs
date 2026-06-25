using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Ability_superalcohol : Ability
    {
        public Ability_superalcohol()
        {
        }

        public Ability_superalcohol(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            PreActivate(target);

            Pawn caster = pawn;
            Map map = caster.Map;
            if (map == null) return false;

            Vector3 casterPos = caster.DrawPos;
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            if (target.Thing != null)
                targetPos = target.Thing.DrawPos;

            Vector3 dir = (targetPos - casterPos).normalized;

            int burstCount = 60;
            int ticksBetweenShots = 2;

            for (int i = 0; i < burstCount; i++)
            {
                float progress = (float)i / burstCount;
                float angleOffset = Mathf.Sin(progress * Mathf.PI * 16f) * 35f;

                float baseAngle = dir.AngleFlat() - 90f;
                float finalAngle = baseAngle + angleOffset;
                Vector3 projDir = Vector3Utility.FromAngleFlat(finalAngle);

                Vector3 spawnPos = casterPos + dir * 1f;
                Vector3 projTargetPos = spawnPos + projDir * 20f;
                LocalTargetInfo projTargetInfo = new LocalTargetInfo(projTargetPos.ToIntVec3());

                IntVec3 spawnCell = spawnPos.ToIntVec3();

                Projectile proj = (Projectile)GenSpawn.Spawn(
                    ThingDef.Named("Projectile_FireMistSpray"),
                    spawnCell,
                    map
                );

                proj.Launch(caster, spawnPos, projTargetInfo, projTargetInfo,
                    ProjectileHitFlags.None, false, null, null);
            }

            return true;
        }
    }
}
