using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_FlightTracker), "Notify_JobStarted")]
    public static class Patch_KeepFlyingOnJob
    {
        static bool Prefix(Pawn_FlightTracker __instance, Job job)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return true;

            if (!PawnHasGohei(pawn))
            {
                return true; 
            }

            if (__instance.Flying)
            {
                job.flying = true;
                return false; 
            }

            if (__instance.CanFlyNow)
            {
                __instance.StartFlying();
                job.flying = true;
                return false;
            }

            return true;
        }

        private static bool PawnHasGohei(Pawn pawn)
        {
            if (pawn.equipment?.Primary == null) return false;
            return pawn.equipment.Primary.def.defName == "Gohei";
        }
    }
}
