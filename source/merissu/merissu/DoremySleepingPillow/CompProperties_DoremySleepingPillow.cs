using System.Collections.Generic;
using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_DoremySleepingPillow : CompProperties
    {
        public CompProperties_DoremySleepingPillow()
        {
            this.compClass = typeof(Comp_DoremySleepingPillow);
        }
    }

    public class Comp_DoremySleepingPillow : ThingComp
    {
        public CompProperties_DoremySleepingPillow Props => (CompProperties_DoremySleepingPillow)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            CheckAndApplyHediff();
        }

        private void CheckAndApplyHediff()
        {
            if (parent.Map == null) return;

            float radius = 1.5f;
            IntVec3 center = parent.Position;
            Map map = parent.Map;

            HediffDef dreamHediff = HediffDef.Named("DreamExpress");
            if (dreamHediff == null) return;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;

                List<Thing> thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is Pawn pawn)
                    {
                        if (pawn.RaceProps.Humanlike && pawn.InBed() && !pawn.health.hediffSet.HasHediff(dreamHediff))
                        {
                            pawn.health.AddHediff(dreamHediff);
                        }
                    }
                }
            }
        }
    }
}