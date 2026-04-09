using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_CentipedeLogic : HediffCompProperties
    {
        public HediffCompProperties_CentipedeLogic()
        {
            compClass = typeof(HediffComp_CentipedeLogic);
        }
    }

    public class HediffComp_CentipedeLogic : HediffComp
    {
        private const int CheckInterval = 60;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(CheckInterval))
            {
                UpdateSeverity();
            }
        }

        private void UpdateSeverity()
        {
            float fullPowerSev = GetHediffSeverity("FullPower");
            float upSev = GetHediffSeverity("up");
            float total = fullPowerSev + upSev;

            float newSeverity;

            if (total >= 20f) newSeverity = 5.0f;
            else if (total >= 16f) newSeverity = 4.5f;
            else if (total >= 12f) newSeverity = 3.5f;
            else if (total >= 8f) newSeverity = 2.5f;
            else if (total >= 4f) newSeverity = 1.5f;
            else newSeverity = 0.5f;

            parent.Severity = newSeverity;
        }

        private float GetHediffSeverity(string defName)
        {
            Hediff firstHediffOfDef = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(defName));
            return firstHediffOfDef?.Severity ?? 0f;
        }
    }
}