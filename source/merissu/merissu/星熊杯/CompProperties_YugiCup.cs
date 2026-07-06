using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_YugiCup : CompProperties
    {
        public float alcoholCapacity = 10f;

        public float alcoholConsumptionPerTick = 1f / 60000f;

        public HediffDef intoxicationHediff;

        public float severityPerConsumption = 0.05f;

        public ThingFilter alcoholFilter;

        public CompProperties_YugiCup()
        {
            compClass = typeof(CompYugiCup);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (alcoholFilter != null)
                alcoholFilter.ResolveReferences();
        }
    }
}
