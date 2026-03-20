using Verse;

namespace merissu
{
    public class HediffComp_Heal : HediffComp
    {
        public int tick;

        public HediffCompProperties_Heal Props => (HediffCompProperties_Heal)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (tick % 2 == 0 && !base.Pawn.Dead)
            {
                HealthUtility.FixWorstHealthCondition(base.Pawn);
            }
            tick++;
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tick, "tick", 0);
        }
    }
}