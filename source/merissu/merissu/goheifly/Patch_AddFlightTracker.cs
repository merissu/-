using HarmonyLib;
using merissu;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(Pawn), "Tick")]
public static class Patch_AddFlightTracker
{
    static void Prefix(Pawn __instance)
    {
        if (__instance == null || !__instance.Spawned) return;

        if (__instance.equipment?.Primary?.GetComp<CompGoheiFlight>() != null)
        {
            var tracker = PawnFlightUtility.GetFlightTracker(__instance);
            if (tracker == null)
            {
                typeof(Pawn)
                    .GetField("flightTracker", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(__instance, new Pawn_FlightTracker(__instance));
            }
        }
    }
}