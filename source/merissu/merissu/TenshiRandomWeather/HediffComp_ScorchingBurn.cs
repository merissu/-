using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_ScorchingBurn : HediffComp
    {
        public HediffCompProperties_ScorchingBurn Props => (HediffCompProperties_ScorchingBurn)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(120))
            {
                if (Pawn.IsHashIntervalTick(60))
                {
                    if (Pawn.Spawned && !Pawn.Dead && !Pawn.Position.Roofed(Pawn.Map))
                    {
                        FireUtility.TryAttachFire(Pawn, 0.5f, null);
                    }
                }
            }
        }
    }

    public class HediffCompProperties_ScorchingBurn : HediffCompProperties
    {
        public HediffCompProperties_ScorchingBurn()
        {
            this.compClass = typeof(HediffComp_ScorchingBurn);
        }
    }
}