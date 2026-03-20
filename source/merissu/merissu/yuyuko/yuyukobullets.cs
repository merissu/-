using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace merissu
{
    public class yuyukobullets : Bullet
    {
        protected override void Tick()
        {
            base.Tick();

            if (Destroyed) return;

            CheckAdvancedCollision();
        }

        private void CheckAdvancedCollision()
        {
            Vector3 currentPos = DrawPos;
            IntVec3 intPos = currentPos.ToIntVec3();

            if (!intPos.InBounds(Map)) return;

            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(
                intPos,
                Map,
                0.5f, 
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
    }
}