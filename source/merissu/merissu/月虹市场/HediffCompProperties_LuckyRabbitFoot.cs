using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_LuckyRabbitFoot : HediffCompProperties
    {
        public HediffCompProperties_LuckyRabbitFoot()
        {
            compClass = typeof(HediffComp_LuckyRabbitFoot);
        }
    }

    public class HediffComp_LuckyRabbitFoot : HediffComp
    {
        private float lastFullPowerSeverity = -1f;
        private float lastUpSeverity = -1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(20))
            {
                CheckAndRestoreSeverity("FullPower", ref lastFullPowerSeverity);
                CheckAndRestoreSeverity("up", ref lastUpSeverity);
            }
        }

        private void CheckAndRestoreSeverity(string defName, ref float lastSeverity)
        {
            Hediff targetHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(defName));

            if (targetHediff != null)
            {
                if (lastSeverity > 0 && targetHediff.Severity < lastSeverity)
                {
                    if (Rand.Value < 0.5f)
                    {
                        targetHediff.Severity = lastSeverity;
                    }
                }
                lastSeverity = targetHediff.Severity;
            }
            else
            {
                lastSeverity = -1f;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastFullPowerSeverity, "lastFullPowerSeverity", -1f);
            Scribe_Values.Look(ref lastUpSeverity, "lastUpSeverity", -1f);
        }
    }
}