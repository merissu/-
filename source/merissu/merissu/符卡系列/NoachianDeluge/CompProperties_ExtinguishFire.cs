using System.Collections.Generic;
using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_ExtinguishFire : CompProperties
    {
        public float radius = 1.0f; 

        public CompProperties_ExtinguishFire()
        {
            this.compClass = typeof(CompExtinguishFire);
        }
    }

    public class CompExtinguishFire : ThingComp
    {
        public CompProperties_ExtinguishFire Props => (CompProperties_ExtinguishFire)this.props;

        public override void CompTick()
        {
            base.CompTick();

            if (!this.parent.Spawned) return;

            Map map = this.parent.Map;
            IntVec3 currentPos = this.parent.Position;

            if (!map.fireWatcher.LargeFireDangerPresent)
            {
                List<Thing> allFires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
                if (allFires == null || allFires.Count == 0)
                {
                    return;
                }
            }

            if (IsCellOnFire(currentPos, map))
            {
                ExtinguishInRadius(currentPos, map);
                this.parent.Destroy(); 
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap != null)
            {
                ExtinguishInRadius(this.parent.Position, previousMap);
            }
        }

        private bool IsCellOnFire(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map)) return false;

            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing is Fire) return true;
                if (thing.IsBurning()) return true;
            }
            return false;
        }

        private void ExtinguishInRadius(IntVec3 center, Map map)
        {
            if (!center.InBounds(map)) return;

            int cellCount = GenRadial.NumCellsInRadius(Props.radius);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 targetCell = center + GenRadial.RadialPattern[i];
                if (!targetCell.InBounds(map)) continue;

                List<Thing> things = targetCell.GetThingList(map);

                for (int j = things.Count - 1; j >= 0; j--)
                {
                    Thing thing = things[j];

                    if (thing is Fire fire)
                    {
                        fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                    }
                    else if (thing.IsBurning())
                    {
                        for (int k = things.Count - 1; k >= 0; k--)
                        {
                            if (things[k] is Fire attachmentFire)
                            {
                                attachmentFire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                            }
                        }
                    }
                }
            }
        }
    }
}