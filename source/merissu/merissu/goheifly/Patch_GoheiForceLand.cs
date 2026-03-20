using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "FlightTick")]
    public static class Patch_GoheiForceLand
    {
        static readonly FieldInfo pawnField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        static readonly FieldInfo flightStateField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");

        static readonly MethodInfo forceLandMethod =
            AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand");

        static void Prefix(Pawn_FlightTracker __instance)
        {
            Pawn pawn = pawnField.GetValue(__instance) as Pawn;
            if (pawn == null) return;

            ThingWithComps weapon = pawn.equipment?.Primary;
            if (weapon == null) return;

            CompGoheiFlight comp = weapon.GetComp<CompGoheiFlight>();
            if (comp == null) return;

            // 如果飞行被关闭，但当前仍在飞行状态
            if (!comp.FlightEnabled)
            {
                var state = flightStateField.GetValue(__instance);
                if (state != null && state.ToString() == "Flying")
                {
                    forceLandMethod.Invoke(__instance, null);
                }
            }
        }
    }
}
