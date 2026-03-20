using RimWorld;
using System.Reflection;
using Verse;

namespace merissu
{
    public static class PawnFlightUtility
    {
        private static readonly FieldInfo flightTrackerField =
            typeof(Pawn).GetField("flightTracker", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Pawn_FlightTracker GetFlightTracker(Pawn pawn)
        {
            if (pawn == null) return null;
            return flightTrackerField?.GetValue(pawn) as Pawn_FlightTracker;
        }
    }
}
