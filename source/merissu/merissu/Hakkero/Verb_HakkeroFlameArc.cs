using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Verb_HakkeroFlameArc : Verb_Shoot
    {
        private const float ArcAngle = 45f;
        private const int BulletsPerWave = 5;

        private static readonly ThingDef ProjectileDef =
            ThingDef.Named("Projectile_HakkeroFlame");

        private static Material arcMat;

        private static Material ArcMat
        {
            get
            {
                if (arcMat == null)
                {
                    arcMat = new Material(ShaderDatabase.Transparent)
                    {
                        color = new Color(
                            1f,
                            0.4f,
                            0.1f,
                            0.25f)
                    };
                }

                return arcMat;
            }
        }
        public override void DrawHighlight(
            LocalTargetInfo target)
        {
            base.DrawHighlight(target);

            Pawn pawn = CasterPawn;

            if (pawn == null || !target.IsValid)
                return;

            Vector3 center = pawn.DrawPos;
            Vector3 targetPos = target.CenterVector3;

            float radius =
                Mathf.Min(
                    Vector3.Distance(center, targetPos),
                    verbProps.range);

            if (radius < 0.5f)
                return;

            float baseAngle =
                (targetPos - center).AngleFlat();

            Matrix4x4 matrix =
                Matrix4x4.TRS(
                    center,
                    Quaternion.Euler(0f, baseAngle, 0f),
                    new Vector3(radius, 1f, radius));

            Graphics.DrawMesh(
                MeshMaker_Fan.GetFanMesh(ArcAngle),
                matrix,
                ArcMat,
                0);
        }

        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;

            if (pawn?.Map == null)
                return false;

            Map map = pawn.Map;

            Vector3 center = pawn.DrawPos;
            Vector3 targetPos = currentTarget.CenterVector3;

            float radius =
                Vector3.Distance(center, targetPos);

            float baseAngle =
                (targetPos - center).AngleFlat();

            IntVec3 casterPos = pawn.Position;

            for (int i = 0; i < BulletsPerWave; i++)
            {
                float angle =
                    baseAngle +
                    Rand.Range(
                        -ArcAngle * 0.5f,
                         ArcAngle * 0.5f);

                Vector3 dir =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f) *
                    Vector3.forward;

                IntVec3 dest =
                    (center + dir * radius)
                    .ToIntVec3();

                if (!dest.InBounds(map))
                    continue;

                Projectile projectile =
                    (Projectile)GenSpawn.Spawn(
                        ProjectileDef,
                        casterPos,
                        map);

                LocalTargetInfo targetInfo =
                    new LocalTargetInfo(dest);

                projectile.Launch(
                    pawn,
                    center,
                    targetInfo,
                    targetInfo,
                    ProjectileHitFlags.All,
                    false,
                    EquipmentSource);
            }

            return true;
        }
    }
}