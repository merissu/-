using Verse;
using RimWorld;
using UnityEngine;

namespace merissu
{
    public class Verb_DeathButterfliesVolley : Verb_Shoot
    {
        private static readonly float[] AngleOffsets =
        {
            -24f, -12f, 0f, 12f, 24f
        };

        private static readonly string[] ProjectilePerRow =
        {
            "Butterfly_RowA",
            "Butterfly_RowB",
            "Butterfly_RowC",
            "Butterfly_RowD"
        };

        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;
            if (pawn == null || pawn.Map == null)
                return false;

            int currentRow =
                verbProps.burstShotCount - burstShotsLeft;

            if (currentRow < 0 || currentRow >= ProjectilePerRow.Length)
                return true;

            Vector3 origin = pawn.DrawPos;
            Vector3 targetPos = currentTarget.CenterVector3;
            float baseAngle = (targetPos - origin).AngleFlat();

            ThingDef projDef =
                ThingDef.Named(ProjectilePerRow[currentRow]);

            if (projDef == null)
                return true;

            for (int i = 0; i < AngleOffsets.Length; i++)
            {
                float angle = baseAngle + AngleOffsets[i];
                Vector3 dir =
                    Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                IntVec3 dest =
                    (origin + dir * verbProps.range).ToIntVec3();

                LocalTargetInfo target =
                    dest.GetFirstPawn(pawn.Map) is Pawn p
                        ? new LocalTargetInfo(p)
                        : new LocalTargetInfo(dest);

                Projectile proj = (Projectile)GenSpawn.Spawn(
                    projDef, pawn.Position, pawn.Map);

                proj.Launch(
                    pawn,
                    origin,
                    target,
                    target,
                    ProjectileHitFlags.All,
                    false,
                    EquipmentSource);
            }

            return true;
        }
    }
}
