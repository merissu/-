using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace merissu
{
    public class FlowerBullets : Bullet
    {
        private float spinAngle;
        private const float SpinSpeed = 6f;

        public float collisionRadius = 1f;

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
                    collisionRadius,
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
                    this.Impact(b);
                    return;
                }
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            Vector3 pos = this.DrawPos;

            base.Impact(hitThing, blockedByShield);

            SpawnSubBullets(pos, map);
        }

        private void SpawnSubBullets(Vector3 pos, Map map)
        {
            if (map == null) return;

            string nextDefName = "";
            float nextRadius = 0.2f;
            int count = 0;

            if (this.def.defName == "FlowerBullets")
            {
                nextDefName = "FlowerBulletsB";
                nextRadius = 0.5f;
                count = 3;
            }
            else if (this.def.defName == "FlowerBulletsB")
            {
                nextDefName = "FlowerBulletsC";
                nextRadius = 0.2f;
                count = 9;
            }
            else if (this.def.defName == "FlowerBulletsC")
            {
                nextDefName = "FlowerBulletsD";
                nextRadius = 0.2f;
                count = 27;
            }
            else
            {
                return;
            }

            ThingDef nextDef = ThingDef.Named(nextDefName);
            if (nextDef == null) return;

            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Vector3 targetPos = pos + direction * 30f; 
                IntVec3 targetCell = targetPos.ToIntVec3();

                FlowerBullets subBullet = (FlowerBullets)GenSpawn.Spawn(nextDef, pos.ToIntVec3(), map);

                subBullet.launcher = this.launcher;
                subBullet.collisionRadius = nextRadius;

                subBullet.Launch(
                    this.launcher,
                    pos,
                    new LocalTargetInfo(targetCell),
                    new LocalTargetInfo(targetCell),
                    ProjectileHitFlags.All,
                    false, 
                    null
                );
            }
        }
    }
}