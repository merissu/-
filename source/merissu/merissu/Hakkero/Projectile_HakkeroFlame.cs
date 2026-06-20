using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Projectile_HakkeroFlame : Projectile
    {
        private Vector3 startPos;
        private bool initialized;

        private const float MaxDistance = 20f;
        private const float MaxDistanceSq = MaxDistance * MaxDistance;

        private const float DamageStartDist = 1.5f;
        private const float DamageStartDistSq = DamageStartDist * DamageStartDist;

        private int fireTickCounter;

        private static readonly MaterialPropertyBlock MPB =
            new MaterialPropertyBlock();

        protected override void Tick()
        {
            base.Tick();

            if (!initialized)
            {
                startPos = ExactPosition;
                initialized = true;
            }

            if (Map == null)
                return;

            Vector3 offset = ExactPosition - startPos;

            float distSq = offset.sqrMagnitude;

            if (distSq > DamageStartDistSq)
            {
                if (++fireTickCounter >= 3)
                {
                    fireTickCounter = 0;

                    IntVec3 cell = Position;

                    FireUtility.TryStartFireIn(
                        cell,
                        Map,
                        0.15f,
                        launcher);

                    Pawn pawn =
                        cell.GetFirstPawn(Map);

                    if (pawn != null &&
                        pawn != launcher &&
                        !pawn.Dead)
                    {
                        pawn.TakeDamage(
                            new DamageInfo(
                                DamageDefOf.Flame,
                                1f,
                                0f,
                                -1f,
                                launcher));
                    }
                }
            }

            if (distSq >= MaxDistanceSq)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void DrawAt(
            Vector3 drawLoc,
            bool flip = false)
        {
            Vector3 offset = ExactPosition - startPos;

            float progress =
                Mathf.Clamp01(
                    offset.sqrMagnitude /
                    MaxDistanceSq);

            float alpha =
                1f - progress * progress * progress;

            Material mat = Graphic.MatSingle;

            MPB.Clear();

            MPB.SetColor(
                ShaderPropertyIDs.Color,
                new Color(
                    1f,
                    0.6f * alpha,
                    0.3f * alpha,
                    alpha));

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(
                    drawLoc,
                    ExactRotation,
                    Vector3.one),
                mat,
                0,
                null,
                0,
                MPB);
        }
    }
}