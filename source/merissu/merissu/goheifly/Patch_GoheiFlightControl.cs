using HarmonyLib;
using Verse;
using RimWorld;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "CanEverFly", MethodType.Getter)]
    public static class Patch_GoheiFlightControl
    {
        static void Postfix(Pawn ___pawn, ref bool __result)
        {
            if (!__result) return;

            ThingWithComps weapon = ___pawn?.equipment?.Primary;
            if (weapon != null)
            {
                CompGoheiFlight comp = weapon.GetComp<CompGoheiFlight>();
                if (comp != null)
                {
                    __result = comp.FlightEnabled;
                }
            }
        }
    }
}