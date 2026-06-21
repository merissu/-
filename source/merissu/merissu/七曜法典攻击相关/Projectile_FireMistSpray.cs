using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Projectile_FireMistSpray : Projectile
    {
        private Vector3 startPos;
        private bool initialized;

        private const float MaxDistance = 18f; 
        private const float MaxDistanceSq = MaxDistance * MaxDistance;

        private const float DamageStartDist = 1.5f;
        private const float DamageStartDistSq = DamageStartDist * DamageStartDist;

        private int fireTickCounter;

        private static readonly MaterialPropertyBlock MPB = new MaterialPropertyBlock();

        protected override void Tick()
        {
            base.Tick();

            if (!initialized)
            {
                startPos = ExactPosition;
                initialized = true;
            }

            if (Map == null) return;

            Vector3 offset = ExactPosition - startPos;
            float distSq = offset.sqrMagnitude;

            if (distSq > DamageStartDistSq)
            {
                if (++fireTickCounter >= 4)
                {
                    fireTickCounter = 0;
                    IntVec3 cell = Position;

                    if (cell.InBounds(Map))
                    {
                        FireUtility.TryStartFireIn(cell, Map, 0.15f, launcher);

                        List<Thing> thingList = cell.GetThingList(Map);
                        for (int i = 0; i < thingList.Count; i++)
                        {
                            Thing t = thingList[i];
                            if (t is Pawn pawn && pawn != launcher && !pawn.Dead)
                            {
                                pawn.TakeDamage(new DamageInfo(
                                    DamageDefOf.Flame,
                                    1f,
                                    0f,
                                    -1f,
                                    launcher));
                            }
                        }
                    }
                }
            }

            if (distSq >= MaxDistanceSq)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!initialized) return;

            Vector3 offset = ExactPosition - startPos;

            float progress = Mathf.Clamp01(offset.sqrMagnitude / MaxDistanceSq);

            float alpha = 1f - (progress * progress * progress);

            Material mat = Graphic.MatSingle;
            MPB.Clear();

            MPB.SetColor(ShaderPropertyIDs.Color, new Color(1f, 0.6f * alpha, 0.3f * alpha, alpha));

            Vector3 scale = new Vector3(this.def.graphicData.drawSize.x, 1f, this.def.graphicData.drawSize.y);

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(drawLoc, ExactRotation, scale),
                mat,
                0,
                null,
                0,
                MPB
            );
        }
    }
}