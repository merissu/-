using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            var harmony = new Harmony("merissu.warflyer.patch");
            harmony.PatchAll();

        }
    }

    public static class FlightUtil
    {
        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, Pawn>("pawn");

        public static Pawn GetPawn(Pawn_FlightTracker tracker) => PawnRef(tracker);

        public static bool IsWarFlyer(Pawn pawn)
        {
            return pawn != null && pawn.def == MerissuDefOf.Merissu_WarFlyer;
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "get_MaxFlightTicks")]
    public static class Patch_MaxFlightTicks
    {
        public static void Postfix(Pawn_FlightTracker __instance, ref int __result)
        {
            Pawn pawn = FlightUtil.GetPawn(__instance);
            if (FlightUtil.IsWarFlyer(pawn))
            {
                __result = int.MaxValue / 8;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.ForceLand))]
    public static class Patch_ForceLand
    {
        public static bool Prefix(Pawn_FlightTracker __instance)
        {
            Pawn pawn = FlightUtil.GetPawn(__instance);
            if (FlightUtil.IsWarFlyer(pawn))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static class Patch_NotifyJobStarted
    {
        public static void Postfix(Pawn_FlightTracker __instance, Job job)
        {
            Pawn pawn = FlightUtil.GetPawn(__instance);
            if (!FlightUtil.IsWarFlyer(pawn) || job == null) return;

            if (__instance.CanFlyNow)
            {
                __instance.StartFlying();
                job.flying = true;
            }
            else if (__instance.Flying)
            {
                job.flying = true;
            }
        }
    }
}