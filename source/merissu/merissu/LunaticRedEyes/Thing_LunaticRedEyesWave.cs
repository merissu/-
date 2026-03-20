using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class Thing_LunaticRedEyesWave : Thing
    {
        private Pawn caster;
        private float radius;
        private float alpha = 1f;

        private const float MaxRadius = 40f;
        private const float ExpandSpeed = 0.6f;

        private HashSet<Pawn> affectedPawns = new HashSet<Pawn>();

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();

            radius += ExpandSpeed;
            alpha = 1f - (radius / MaxRadius);
            if (alpha < 0f) alpha = 0f;

            AffectPawns();

            if (radius >= MaxRadius)
            {
                Destroy();
            }
        }

        private void AffectPawns()
        {
            var pawns = Map.mapPawns.AllPawnsSpawned;

            foreach (Pawn p in pawns)
            {
                if (p == caster) continue;
                if (p.Faction == caster.Faction) continue;
                if (p.Dead || p.Downed) continue;
                if (affectedPawns.Contains(p)) continue;

                float dist = (p.Position - Position).LengthHorizontal;

                if (dist <= radius)
                {
                    affectedPawns.Add(p);

                    p.mindState?.mentalStateHandler?
                        .TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Material mat = MaterialPool.MatFrom(
                "Projectiles/RedEyes",
                ShaderDatabase.Transparent,
                new Color(1f, 1f, 1f, alpha));

            float size = radius * 2f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc + Vector3.up * 0.1f,
                Quaternion.identity,
                new Vector3(size, 1f, size));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}
