using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace merissu
{
    public static class HighClearSkyFlightUtil
    {
        public static readonly HediffDef HighClearSkyFlyingDef = HediffDef.Named("HighClearSky_Flying");

        public static bool HasHighClearSkyFlying(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HighClearSkyFlyingDef) != null;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_HighClearSky_AddFlightTracker
    {
        static void Prefix(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned) return;
            if (!HighClearSkyFlightUtil.HasHighClearSkyFlying(__instance)) return;

            if (__instance.flight == null)
            {
                __instance.flight = new Pawn_FlightTracker(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_FlightTracker), "CanEverFly", MethodType.Getter)]
    public static class Patch_HighClearSky_CanEverFly
    {
        private static readonly FieldInfo pawnField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        static void Postfix(Pawn_FlightTracker __instance, ref bool __result)
        {
            var pawn = pawnField.GetValue(__instance) as Pawn;
            if (HighClearSkyFlightUtil.HasHighClearSkyFlying(pawn))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "Notify_JobStarted")]
    public static class Patch_HighClearSky_KeepFlyingOnJob
    {
        private static readonly FieldInfo pawnField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        private static readonly FieldInfo flightCooldownTicksField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightCooldownTicks");

        static bool Prefix(Pawn_FlightTracker __instance, Job job)
        {
            var pawn = pawnField.GetValue(__instance) as Pawn;
            if (!HighClearSkyFlightUtil.HasHighClearSkyFlying(pawn))
                return true; 

            flightCooldownTicksField.SetValue(__instance, 0);

            if (!__instance.Flying && __instance.CanFlyNow)
                __instance.StartFlying();

            if (job != null)
                job.flying = true;

            return false; 
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "FlightTick")]
    public static class Patch_HighClearSky_ForceStayFlying
    {
        private static readonly FieldInfo pawnField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "pawn");

        private static readonly FieldInfo flightStateField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");

        private static readonly FieldInfo flyingTicksField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flyingTicks");

        private static readonly FieldInfo lerpTickField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "lerpTick");

        private static readonly FieldInfo flightCooldownTicksField =
            AccessTools.Field(typeof(Pawn_FlightTracker), "flightCooldownTicks");

        static void Postfix(Pawn_FlightTracker __instance)
        {
            var pawn = pawnField.GetValue(__instance) as Pawn;
            if (!HighClearSkyFlightUtil.HasHighClearSkyFlying(pawn))
                return;

            object stateObj = flightStateField.GetValue(__instance);
            string state = stateObj?.ToString();

            if (state == "Landing" || state == "Grounded")
            {
                Type enumType = flightStateField.FieldType;
                object flyingEnum = Enum.Parse(enumType, "Flying");

                flightStateField.SetValue(__instance, flyingEnum);
                lerpTickField.SetValue(__instance, 0);
                flyingTicksField.SetValue(__instance, 0);
                flightCooldownTicksField.SetValue(__instance, 0);
                return;
            }

            if (state == "Flying")
            {
                int flyingTicks = (int)flyingTicksField.GetValue(__instance);
                int max = __instance.MaxFlightTicks;
                if (max > 0 && flyingTicks >= max - 1)
                {
                    flyingTicksField.SetValue(__instance, 0);
                }
            }

            if (!__instance.Flying)
            {
                flightCooldownTicksField.SetValue(__instance, 0);
                if (__instance.CanFlyNow)
                    __instance.StartFlying();
            }
        }
    }
}