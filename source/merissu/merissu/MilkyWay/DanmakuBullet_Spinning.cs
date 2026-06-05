using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace merissu
{
    public class DanmakuBullet_Spinning : Bullet
    {
        private float spinAngle;
        private const float SpinSpeed = 2f;

        private float cachedHitRadius = -1f;

        protected float BulletHitRadius
        {
            get
            {
                if (cachedHitRadius < 0f)
                {
                    float drawSizeX = this.def.graphicData?.drawSize.x ?? 1f;
                    float visualRadius = drawSizeX / 2f;

                    cachedHitRadius = visualRadius * 0.3333f;
                }
                return cachedHitRadius;
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (Destroyed) return;

            spinAngle += SpinSpeed;
            if (spinAngle >= 360f)
                spinAngle -= 360f;

            CheckAdvancedCollision();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Quaternion flightRotation = ExactRotation;
            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            Quaternion finalRotation = flightRotation * spinRotation;

            Vector2 s = this.Graphic.drawSize;
            Vector3 scale = new Vector3(s.x, 1f, s.y);

            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, finalRotation, scale);

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                this.Graphic.MatSingle,
                0
            );
        }

        private void CheckAdvancedCollision()
        {
            Vector3 exactPos = this.ExactPosition;
            IntVec3 intPos = exactPos.ToIntVec3();

            if (!intPos.InBounds(Map)) return;

            float searchRadius = Mathf.Max(1.2f, this.def.graphicData?.drawSize.x ?? 1f);
            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(intPos, Map, searchRadius, true);

            foreach (Thing thing in list)
            {
                if (thing == launcher) continue;

                if (thing is Pawn p && !p.Dead && p.Faction != launcher?.Faction)
                {
                    Vector3 targetPos = p.DrawPos;
                    targetPos.y = exactPos.y; 

                    float distance = Vector3.Distance(exactPos, targetPos);
                    float targetHitRadius;

                    if (State.IsActive && State.PC?.pawn != null && p == State.PC.pawn)
                    {
                        targetHitRadius = STG_HitManager.HitboxHalfWidth; 
                    }
                    else
                    {
                        targetHitRadius = 0.4f; 
                    }

                    if (distance <= (this.BulletHitRadius + targetHitRadius))
                    {
                        this.Impact(p);
                        return;
                    }
                }
                else if (thing is Building b && b.def.fillPercent > 0)
                {
                    if (b is Building_Turret && launcher != null && b.Faction == launcher.Faction) continue;

                    if (b.OccupiedRect().Contains(intPos))
                    {
                        this.Impact(b);
                        return;
                    }
                }
            }
        }
    }
}