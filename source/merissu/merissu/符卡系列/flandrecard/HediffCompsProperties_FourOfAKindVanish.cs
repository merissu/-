using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_FourOfAKindVanish : HediffCompProperties
    {
        public HediffCompProperties_FourOfAKindVanish()
        {
            this.compClass = typeof(HediffComps_FourOfAKindVanish);
        }
    }

    public class HediffComps_FourOfAKindVanish : HediffComp
    {
        public HediffCompProperties_FourOfAKindVanish Props => (HediffCompProperties_FourOfAKindVanish)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null) return;

            if (parent.Severity <= 0.01f)
            {
                DoVanish();
                return;
            }

            if (Pawn.Dead || Pawn.Downed)
            {
                DoVanish();
            }
        }

        private void DoVanish()
        {
            Pawn victim = Pawn;

            if (!victim.Destroyed)
            {
                victim.Destroy(DestroyMode.Vanish);
            }

            if (Find.WorldPawns.Contains(victim))
            {
                Find.WorldPawns.RemovePawn(victim);
            }
        }
    }
}