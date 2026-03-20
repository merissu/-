using Verse;
using UnityEngine;
using RimWorld;

namespace merissu
{
    public class Projectile_HakkeroFlame : Projectile
    {
        private Vector3 startPos;
        private bool initialized;

        public float maxDistance = 20f;

        protected override void Tick()
        {
            base.Tick();

            if (!initialized)
            {
                startPos = ExactPosition;
                initialized = true;
            }

            if (Map == null) return;

            IntVec3 curCell = Position;

            float currentDist = Vector3.Distance(startPos, ExactPosition);

            if (currentDist > 1.5f)
            {
                FireUtility.TryStartFireIn(curCell, Map, 1f, launcher);

                Pawn p = curCell.GetFirstPawn(Map);
                if (p != null && !p.Dead && p != launcher)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, 1f, 0f, -1f, launcher);
                    p.TakeDamage(dinfo);
                }
            }

            if (currentDist >= maxDistance)
            {
                Destroy(DestroyMode.Vanish);
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float dist = Vector3.Distance(startPos, ExactPosition);
            float progress = Mathf.Clamp01(dist / maxDistance);

            float alpha = 1f - Mathf.Pow(progress, 3);

            Material mat = Graphic.MatSingle;

            mat.color = new Color(1f, 0.6f * alpha, 0.3f * alpha, alpha);

            Graphics.DrawMesh(
                MeshPool.plane10,
                drawLoc,
                ExactRotation,
                mat,
                0);
        }
    }
}
