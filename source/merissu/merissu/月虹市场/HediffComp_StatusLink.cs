using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_MountainGodLogic : HediffComp
    {
        private float lastFullPower = -1f;
        private float lastUp = -1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(10))
            {
                SyncLogic();
            }
        }

        private void SyncLogic()
        {
            Hediff fp = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            Hediff up = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("up"));

            float curFP = fp?.Severity ?? 0f;
            float curUp = up?.Severity ?? 0f;

            if (lastFullPower == -1f) { lastFullPower = curFP; lastUp = curUp; return; }

            float diffFP = curFP - lastFullPower;
            float diffUp = curUp - lastUp;

            if (diffFP > 0.001f)
            {
                ApplyEffect(HediffDef.Named("up"), diffFP);
                lastFullPower = curFP;
                lastUp = (Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("up"))?.Severity ?? 0f);
            }
            else if (diffUp > 0.001f)
            {
                ApplyEffect(HediffDef.Named("FullPower"), diffUp);
                lastUp = curUp;
                lastFullPower = (Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"))?.Severity ?? 0f);
            }
            else
            {
                lastFullPower = curFP;
                lastUp = curUp;
            }
        }

        private void ApplyEffect(HediffDef def, float amount)
        {
            Hediff h = Pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (h == null)
            {
                Pawn.health.AddHediff(def);
                Hediff newH = Pawn.health.hediffSet.GetFirstHediffOfDef(def);
                if (newH != null) newH.Severity = amount;
            }
            else
            {
                h.Severity += amount;
            }
        }
    }
    public class HediffCompProperties_MountainGodLogic : HediffCompProperties
    {
        public HediffCompProperties_MountainGodLogic()
        {
            this.compClass = typeof(HediffComp_MountainGodLogic);
        }
    }

}