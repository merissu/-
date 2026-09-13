using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace merissu
{
    public static class FlightCompatUtility
    {
        private static readonly FieldInfo FlightField =
            AccessTools.Field(typeof(Pawn), "flight");

        private static readonly HediffDef SpiritualPowerDef =
            HediffDef.Named("spiritualpower");

        public static Pawn_FlightTracker EnsureFlightTracker(Pawn pawn)
        {
            if (pawn == null)
                return null;

            if (pawn.flight != null)
                return pawn.flight;

            var tracker = new Pawn_FlightTracker(pawn);
            FlightField?.SetValue(pawn, tracker);
            return tracker;
        }

        public static bool IsAnyFlightEnabled(Pawn pawn)
        {
            if (pawn == null)
                return false;

            var weapon = pawn.equipment?.Primary;

            if (weapon != null)
            {
                var goheiComp = weapon.GetComp<CompGoheiFlight>();

                if (goheiComp != null &&
                    goheiComp.FlightEnabled)
                {
                    return true;
                }
            }

            if (pawn.health != null)
            {
                var power =
                    pawn.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef);

                if (power != null)
                {
                    var flightComp =
                        power.TryGetComp<HediffComp_SpiritualFlightToggle>();

                    if (flightComp != null &&
                        flightComp.flightEnabled)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
