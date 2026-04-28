using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;
using System.Reflection;

namespace merissu
{
    public static class HighClearSkyFlightUtil
    {
        public static readonly HediffDef HighClearSkyFlyingDef = HediffDef.Named("HighClearSky_Flying");

        public static bool HasHighClearSkyFlying(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(HighClearSkyFlyingDef) ?? false;
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "FlightTick")]
    public static class Patch_HighClearSky_ForceStayFlying
    {
        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, Pawn>("pawn");

        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, int> FlyingTicksRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, int>("flyingTicks");

        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, int> LerpTickRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, int>("lerpTick");

        private static readonly AccessTools.FieldRef<Pawn_FlightTracker, int> FlightCooldownTicksRef =
            AccessTools.FieldRefAccess<Pawn_FlightTracker, int>("flightCooldownTicks");

        private static readonly FieldInfo flightStateField = AccessTools.Field(typeof(Pawn_FlightTracker), "flightState");

        private static object flyingEnum;
        private static int flyingInt;
        private static int landingInt;
        private static int groundedInt;
        private static bool enumsInitialized = false;

        static void Postfix(Pawn_FlightTracker __instance)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn == null || !HighClearSkyFlightUtil.HasHighClearSkyFlying(pawn))
                return;

            if (!enumsInitialized)
            {
                Type enumType = flightStateField.FieldType;
                flyingEnum = Enum.Parse(enumType, "Flying");
                flyingInt = (int)flyingEnum;
                landingInt = (int)Enum.Parse(enumType, "Landing");
                groundedInt = (int)Enum.Parse(enumType, "Grounded");
                enumsInitialized = true;
            }

            int currentState = (int)flightStateField.GetValue(__instance);

            if (currentState == landingInt || currentState == groundedInt)
            {
                flightStateField.SetValue(__instance, flyingEnum);
                LerpTickRef(__instance) = 0;
                FlyingTicksRef(__instance) = 0;
                FlightCooldownTicksRef(__instance) = 0;
                return;
            }

            if (currentState == flyingInt)
            {
                int max = __instance.MaxFlightTicks;
                if (max > 0 && FlyingTicksRef(__instance) >= max - 1)
                {
                    FlyingTicksRef(__instance) = 0;
                }
            }

            if (!__instance.Flying)
            {
                FlightCooldownTicksRef(__instance) = 0;
                if (__instance.CanFlyNow)
                    __instance.StartFlying();
            }
        }
    }
}