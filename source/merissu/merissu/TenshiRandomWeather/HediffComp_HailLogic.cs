using RimWorld;
using System.Linq;
using Verse;

namespace merissu
{
    public class HediffComp_HailLogic : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            Pawn pawn = parent.pawn;
            Hediff fullPower = pawn.health.hediffSet.hediffs
                .FirstOrDefault(x => x.def.defName == "FullPower");

            if (fullPower != null)
            {
                fullPower.Severity *= 2f;
            }
            else
            {
                HediffDef fullPowerDef = HediffDef.Named("FullPower");
                Hediff newHediff = HediffMaker.MakeHediff(fullPowerDef, pawn);
                newHediff.Severity = 1f; 
                pawn.health.AddHediff(newHediff);
            }
        }
    }
    public class HediffCompProperties_HailLogic : HediffCompProperties
    {
        public HediffCompProperties_HailLogic()
        {
            this.compClass = typeof(HediffComp_HailLogic);
        }
    }
}