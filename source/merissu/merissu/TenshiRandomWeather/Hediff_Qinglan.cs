using Verse;
using RimWorld;

namespace merissu
{
    public class Hediff_Qinglan : HediffWithComps
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            HediffDef fullPowerDef = HediffDef.Named("FullPower");

            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(fullPowerDef);

            if (firstHediffOfDef == null)
            {
                firstHediffOfDef = pawn.health.AddHediff(fullPowerDef);
            }

            if (firstHediffOfDef != null)
            {
                firstHediffOfDef.Severity = Rand.RangeInclusive(1, 5);
            }

        }
    }
}