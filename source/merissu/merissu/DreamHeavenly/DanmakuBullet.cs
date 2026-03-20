using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace merissu
{
    public class DanmakuBullet : Bullet
    {
        protected override void Tick()
        {
            base.Tick();

            if (this.Destroyed) return;

            CheckAdvancedCollision();
        }

        private void CheckAdvancedCollision()
        {
            var currentPos = this.DrawPos;
            IntVec3 intPos = currentPos.ToIntVec3();

            if (!intPos.InBounds(base.Map)) return;

            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(intPos, base.Map, 0.8f, true);

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