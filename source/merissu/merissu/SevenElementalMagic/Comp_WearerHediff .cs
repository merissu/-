using Verse;
using RimWorld;

namespace merissu
{
    public class Comp_WearerHediff : ThingComp
    {
        public CompProperties_WearerHediff Props =>
            (CompProperties_WearerHediff)props;

        private Pawn cachedWearer;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            cachedWearer = pawn;
            TryAddHediff(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            RemoveHediff(pawn);
            cachedWearer = null;
            base.Notify_Unequipped(pawn);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (cachedWearer != null)
                RemoveHediff(cachedWearer);

            base.PostDestroy(mode, previousMap);
        }

        private void TryAddHediff(Pawn pawn)
        {
            if (pawn == null || Props.hediff == null)
                return;

            if (!pawn.health.hediffSet.HasHediff(Props.hediff))
            {
                pawn.health.AddHediff(Props.hediff);
            }
        }

        private void RemoveHediff(Pawn pawn)
        {
            if (pawn == null || Props.hediff == null)
                return;

            var h = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
            if (h != null)
                pawn.health.RemoveHediff(h);
        }
    }
}
