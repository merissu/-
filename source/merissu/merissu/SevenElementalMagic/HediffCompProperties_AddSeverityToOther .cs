using Verse;

namespace merissu
{
    public class HediffCompProperties_AddSeverityToOther : HediffCompProperties
    {
        public HediffDef targetHediff;
        public float severityPerDay = 1f;

        public HediffCompProperties_AddSeverityToOther()
        {
            compClass = typeof(HediffComp_AddSeverityToOther);
        }
    }
}
