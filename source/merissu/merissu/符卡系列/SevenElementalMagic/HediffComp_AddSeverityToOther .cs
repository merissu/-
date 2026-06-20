using Verse;

namespace merissu
{
    public class HediffComp_AddSeverityToOther : HediffComp
    {
        private int lastTick = -1;

        public HediffCompProperties_AddSeverityToOther Props =>
            (HediffCompProperties_AddSeverityToOther)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent.pawn;
            if (pawn == null || Props.targetHediff == null)
                return;

            if (Find.TickManager.TicksGame % 60000 != 0)
                return;

            Hediff target = pawn.health.hediffSet.GetFirstHediffOfDef(Props.targetHediff);

            if (target != null)
            {
                target.Severity += Props.severityPerDay;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(Props.targetHediff, pawn);

                newHediff.Severity = 1f;

                pawn.health.AddHediff(newHediff);
            }
        }
    }
}
