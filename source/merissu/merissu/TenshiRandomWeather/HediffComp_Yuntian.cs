using RimWorld;
using System.Linq;
using Verse;

namespace merissu
{
    public class HediffComp_Yuntian : HediffComp
    {
        private float lastPowerSeverity = 0f;

        private Hediff FullPowerHediff => parent.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (parent.pawn.IsHashIntervalTick(10))
            {
                var currentFullPower = FullPowerHediff;

                if (currentFullPower != null)
                {
                    float currentSeverity = currentFullPower.Severity;

                    float decrease = lastPowerSeverity - currentSeverity;

                    if (decrease > 1.0f)
                    {
                        currentFullPower.Severity += 1.0f;

                        MoteMaker.ThrowText(parent.pawn.DrawPos, parent.pawn.Map, "云天：灵力返还");
                    }

                    lastPowerSeverity = currentFullPower.Severity;
                }
                else
                {
                    lastPowerSeverity = 0f;
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastPowerSeverity, "lastPowerSeverity", 0f);
        }
    }

    public class HediffCompProperties_Yuntian : HediffCompProperties
    {
        public HediffCompProperties_Yuntian()
        {
            this.compClass = typeof(HediffComp_Yuntian);
        }
    }
}