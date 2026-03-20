using RimWorld;
using Verse;
using System.Collections.Generic;

namespace merissu
{
    public class HediffCompProperties_OnDeathTrigger : HediffCompProperties
    {
        public SoundDef soundDef;
        public List<ThingDef> thingDefs;
        public int stackCount = 1;

        public HediffCompProperties_OnDeathTrigger()
        {
            compClass = typeof(HediffComp_OnDeathTrigger);
        }
    }
}
