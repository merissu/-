using Verse;

namespace merissu
{
    public class HediffComp_DisableMove : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn.pather?.StopDead();
        }
    }

    public class HediffCompProperties_DisableMove : HediffCompProperties
    {
        public HediffCompProperties_DisableMove()
        {
            compClass = typeof(HediffComp_DisableMove);
        }
    }
}
