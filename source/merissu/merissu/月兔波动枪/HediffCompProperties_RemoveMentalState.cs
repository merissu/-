using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_RemoveMentalState : HediffCompProperties
    {
        public MentalStateDef mentalState;

        public HediffCompProperties_RemoveMentalState()
        {
            compClass = typeof(HediffComp_RemoveMentalState);
        }
    }

    public class HediffComp_RemoveMentalState : HediffComp
    {
        public HediffCompProperties_RemoveMentalState Props => (HediffCompProperties_RemoveMentalState)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (Pawn != null && !Pawn.Dead && Pawn.MentalState != null)
            {
                if (Props.mentalState == null || Pawn.MentalState.def == Props.mentalState)
                {
                    Pawn.MentalState.RecoverFromState();
                }
            }
        }
    }
}