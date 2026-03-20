using Verse;
using RimWorld;

namespace merissu
{
    public class CompEquipGrantFlight : ThingComp
    {
        private static readonly HediffDef FlyingHediffDef =
            HediffDef.Named("Hediff_GoheiFlying");

        public override void Notify_Equipped(Pawn pawn)
        {
            if (pawn == null) return;

            Pawn_FlightTracker tracker = PawnFlightUtility.GetFlightTracker(pawn);
            if (tracker == null) return;

            if (tracker.CanFlyNow)
            {
                tracker.StartFlying();
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;

            PawnFlightUtility.GetFlightTracker(pawn)?.ForceLand();

            Hediff flying = pawn.health.hediffSet
                .GetFirstHediffOfDef(FlyingHediffDef);

            if (flying != null)
            {
                pawn.health.RemoveHediff(flying);
            }
        }
    }
}
