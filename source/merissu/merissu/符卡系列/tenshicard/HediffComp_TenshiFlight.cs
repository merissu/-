using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_TenshiFlight : HediffCompProperties
    {
        public HediffDef flyingHediffDef;

        public float flightHeightFactor = 3f;

        public float flightShadowOpacity = 0.85f;

        public float flightShadowScale = 0.75f;

        public int flightShadowPasses = 1;

        public HediffCompProperties_TenshiFlight()
        {
            compClass = typeof(HediffComp_TenshiFlight);
        }
    }
    public class HediffComp_TenshiFlight : HediffComp
    {
        public HediffCompProperties_TenshiFlight Props =>
            (HediffCompProperties_TenshiFlight)props;

        private Pawn P => parent.pawn;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            EnsureFlight();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = P;
            if (pawn == null || !pawn.Spawned || pawn.Dead)
                return;

            if (pawn.pather != null && pawn.pather.Moving)
                pawn.pather.StopDead();

            EnsureFlight();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            StopFlight();
        }

        private void EnsureFlight()
        {
            Pawn pawn = P;
            if (pawn == null || pawn.health == null)
                return;

            if (pawn.Downed)
                return;

            HediffDef flyDef = Props.flyingHediffDef;
            if (flyDef != null &&
                pawn.health.hediffSet.GetFirstHediffOfDef(flyDef) == null)
            {
                pawn.health.AddHediff(flyDef);
            }

            Pawn_FlightTracker tracker = FlightCompatUtility.EnsureFlightTracker(pawn);
            if (tracker == null || tracker.Flying)
                return;

            StatDefOf.MaxFlightTime.Worker.ClearCacheForThing(pawn);

            tracker.StartFlying();
        }

        private void StopFlight()
        {
            Pawn pawn = P;
            if (pawn == null || pawn.health == null)
                return;

            if (FlightCompatUtility.IsAnyFlightEnabled(pawn))
                return;

            HediffDef flyDef = Props.flyingHediffDef;
            if (flyDef != null)
            {
                Hediff flyHediff = pawn.health.hediffSet.GetFirstHediffOfDef(flyDef);
                if (flyHediff != null)
                    pawn.health.RemoveHediff(flyHediff);
            }

            Pawn_FlightTracker tracker = FlightCompatUtility.EnsureFlightTracker(pawn);
            if (tracker != null && tracker.Flying)
                tracker.ForceLand();   
        }
    }
}
