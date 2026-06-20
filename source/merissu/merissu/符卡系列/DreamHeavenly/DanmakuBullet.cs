using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class DanmakuBullet : Bullet
    {
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

            if (this.Destroyed) return;

            CheckAdvancedCollision();
        }

        private void CheckAdvancedCollision()
        {
            Vector3 exactPos = this.ExactPosition;
            IntVec3 intPos = exactPos.ToIntVec3();

            if (!intPos.InBounds(base.Map)) return;

            float searchRadius = Mathf.Max(1.2f, this.def.graphicData?.drawSize.x ?? 1f);
            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(intPos, base.Map, searchRadius, true);

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
                    if (b is Building_Turret && b.Faction == launcher?.Faction) continue;

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