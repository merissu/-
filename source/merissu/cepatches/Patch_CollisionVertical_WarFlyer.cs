using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using CombatExtended;
using UnityEngine;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class MerissuCEBootstrap
    {
        static MerissuCEBootstrap()
        {
            var harmony = new Harmony("merissu.warflyer.ce.combined");
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
            return pawn != null && pawn.def.defName == "Merissu_WarFlyer";
        }
    }

    [HarmonyPatch(typeof(CollisionVertical), "CalculateHeightRange")]
    public static class Patch_CollisionVertical_WarFlyer
    {
        public static void Postfix(Thing thing, ref FloatRange heightRange, ref float shotHeight)
        {
            if (thing is Pawn pawn && FlightUtil.IsWarFlyer(pawn))
            {
                float minH = 0.8f;
                float maxH = 1.4f;

                heightRange = new FloatRange(minH, maxH);
                shotHeight = (minH + maxH) * 0.5f;
            }
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "GetTargetHeight")]
    public static class Patch_Verb_GetTargetHeight_WarFlyer
    {
        public static void Postfix(LocalTargetInfo target, ref float __result)
        {
            if (target.Thing is Pawn pawn && FlightUtil.IsWarFlyer(pawn))
            {
                __result = 0.8f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), "PositionOffsetFactor", MethodType.Getter)]
    public static class Patch_FlightTracker_VisualOffset
    {
        public static void Postfix(Pawn_FlightTracker __instance, ref float __result)
        {
            Pawn pawn = FlightUtil.GetPawn(__instance);
            if (FlightUtil.IsWarFlyer(pawn))
            {
                __result = 0.5f;
            }
        }
    }
}