using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Verb_HakkeroFlameArc : Verb_Shoot
    {
        private const float ArcAngle = 45f;
        private const int BulletsPerWave = 5;

        private static ThingDef CachedProjectileDef;

        private static Material arcMat;
        private static Material ArcMat
        {
            get
            {
                if (arcMat == null)
                {
                    arcMat = new Material(ShaderDatabase.Transparent)
                    {
                        color = new Color(1f, 0.4f, 0.1f, 0.25f)
                    };
                }
                return arcMat;
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            Pawn pawn = CasterPawn;
            if (pawn == null || !target.IsValid) return;

            Vector3 center = pawn.DrawPos;
            Vector3 tpos = target.CenterVector3;
            float radius = Vector3.Distance(center, tpos);

            float maxRange = this.verbProps.range;
            if (radius > maxRange) radius = maxRange;
            if (radius < 0.5f) return;

            float baseAngle = (tpos - center).AngleFlat();

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.Euler(0f, baseAngle, 0f),
                new Vector3(radius, 1f, radius)
            );

            Graphics.DrawMesh(
                MeshMaker_Fan.GetFanMesh(ArcAngle),
                matrix,
                ArcMat,
                0);
        }

        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;
            if (pawn?.Map == null) return false;

            if (CachedProjectileDef == null)
            {
                CachedProjectileDef = ThingDef.Named("Projectile_HakkeroFlame");
            }

            Vector3 center = pawn.DrawPos;
            Vector3 targetPos = currentTarget.CenterVector3;
            float radius = Vector3.Distance(center, targetPos);
            float baseAngle = (targetPos - center).AngleFlat();

            Map map = pawn.Map;
            IntVec3 casterPos = pawn.Position;

            for (int i = 0; i < BulletsPerWave; i++)
            {
                float ang = baseAngle + Rand.Range(-ArcAngle / 2f, ArcAngle / 2f);
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;

                Vector3 destVec = center + dir * radius;
                IntVec3 destCell = destVec.ToIntVec3();

                if (!destCell.InBounds(map)) continue;

                LocalTargetInfo targetInfo = new LocalTargetInfo(destCell);

                Projectile proj = (Projectile)GenSpawn.Spawn(CachedProjectileDef, casterPos, map);

                proj.Launch(pawn, center, targetInfo, targetInfo, ProjectileHitFlags.All, false, EquipmentSource);
            }

            return true;
        }
    }
}