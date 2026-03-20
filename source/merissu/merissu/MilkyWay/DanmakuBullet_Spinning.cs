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
            Vector3 currentPos = DrawPos;
            IntVec3 intPos = currentPos.ToIntVec3();

            if (!intPos.InBounds(Map)) return;

            IEnumerable<Thing> list =
                GenRadial.RadialDistinctThingsAround(
                    intPos,
                    Map,
                    0.8f,
                    true
                );

            foreach (Thing thing in list)
            {
                if (thing == launcher) continue;

                if (thing is Pawn p)
                {
                    if (!p.Dead && p.Faction != launcher.Faction)
                    {
                        this.Impact(p);
                        return;
                    }
                }
                else if (thing is Building b)
                {
                    if (b is Building_Turret)
                    {
                        if (launcher != null && b.Faction != null && b.Faction == launcher.Faction)
                        {
                            continue;
                        }

                    }
                    this.Impact(b);
                    return;
                }
            }
        }
    }
}
