using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "FlightTick")]
    public static class Patch_GoheiForceLand
    {
        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, Pawn>("pawn");

        private static readonly FieldInfo flightStateField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");

        private static readonly MethodInfo forceLandMethod =
            AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand");

        static void Prefix(Pawn_FlightTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn == null) return;

            var weapon = pawn.equipment?.Primary;
            if (weapon == null) return;

            CompGoheiFlight comp = weapon.GetComp<CompGoheiFlight>();

            if (comp == null || comp.FlightEnabled) return;

            object state = flightStateField.GetValue(__instance);

            if (state?.ToString() == "Flying")
            {
                forceLandMethod.Invoke(__instance, null);
            }
        }
    }
}