using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_HakureiShield : CompProperties_Shield
    {
        public HediffDef fullPowerHediff;
        public float maxSeverity = 10f;

        public CompProperties_HakureiShield()
        {
            compClass = typeof(Comp_HakureiShield);
        }
    }
}
