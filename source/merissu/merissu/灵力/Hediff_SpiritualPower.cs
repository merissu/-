using Verse;

namespace merissu
{
    public class Hediff_SpiritualPower : HediffWithComps
    {
        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn != null && !pawn.Destroyed)
            {
                Hediff newHediff = HediffMaker.MakeHediff(this.def, pawn);
                newHediff.Severity = 0.01f;
                pawn.health.AddHediff(newHediff);
            }
        }
    }
}