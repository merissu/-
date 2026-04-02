using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_Sunshower : HediffCompProperties
    {
        public HediffCompProperties_Sunshower()
        {
            compClass = typeof(HediffComp_Sunshower);
        }
    }

    public class HediffComp_Sunshower : HediffComp
    {
        private const int CheckInterval = 2500; 

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(CheckInterval))
            {
                ApplyOrIncrementFullPower();
            }
        }

        private void ApplyOrIncrementFullPower()
        {
            HediffDef fullPowerDef = HediffDef.Named("spiritualpower");
            Hediff existingHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(fullPowerDef);

            if (existingHediff == null)
            {
                Hediff newHediff = HediffMaker.MakeHediff(fullPowerDef, Pawn);
                newHediff.Severity = 0.5f;
                Pawn.health.AddHediff(newHediff);
            }
            else
            {
                existingHediff.Severity += 0.5f;
            }
        }
    }
}