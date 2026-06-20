using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;
using Verse.AI;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "CanEverFly", MethodType.Getter)]
    public static class Patch_UnifiedFlightControl
    {
        static void Postfix(Pawn ___pawn, ref bool __result)
        {
            if (__result)
                return;

            if (FlightCompatUtility.IsAnyFlightEnabled(___pawn))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "Notify_JobStarted")]
    public static class Patch_UnifiedKeepFlyingOnJob
    {
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        static bool Prefix(Pawn_FlightTracker __instance, Job job)
        {
            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (pawn == null) return true;

            bool isFlightEnabled = FlightCompatUtility.IsAnyFlightEnabled(pawn);

            if (!isFlightEnabled)
            {
                if (!__instance.Flying)
                {
                    return false;
                }

                return true;
            }

            if (__instance.Flying)
            {
                if (job != null)
                {
                    job.flying = true; 
                }
                return false;
            }

            return true;
        }
    }
}