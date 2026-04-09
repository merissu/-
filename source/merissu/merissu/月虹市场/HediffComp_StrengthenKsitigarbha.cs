using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_StrengthenKsitigarbha : HediffComp
    {
        private int tickCounter = 0;
        private const int TwoHoursTicks = 5000;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            tickCounter++;
            if (tickCounter >= TwoHoursTicks)
            {
                tickCounter = 0;
                AddOrUpdateUpHediff();
            }
        }

        private void AddOrUpdateUpHediff()
        {
            Pawn pawn = this.Pawn;
            if (pawn == null || pawn.Dead) return;

            HediffDef upDef = HediffDef.Named("up");
            Hediff targetHediff = pawn.health.hediffSet.GetFirstHediffOfDef(upDef);

            if (targetHediff != null)
            {
                targetHediff.Severity += 1.0f;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(upDef, pawn);
                newHediff.Severity = 1.0f;
                pawn.health.AddHediff(newHediff);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }

    public class HediffCompProperties_StrengthenKsitigarbha : HediffCompProperties
    {
        public HediffCompProperties_StrengthenKsitigarbha()
        {
            this.compClass = typeof(HediffComp_StrengthenKsitigarbha);
        }
    }
}