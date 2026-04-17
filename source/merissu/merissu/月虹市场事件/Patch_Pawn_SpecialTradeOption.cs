using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.merissu.market");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_TraderTracker), "GiveSoldThingToTrader")]
    public static class Patch_Pawn_TraderTracker_GiveSoldThingToTrader
    {
        public static bool Prefix(Pawn_TraderTracker __instance, Pawn ___pawn, Pawn playerNegotiator)
        {
            if (___pawn != null && ___pawn.health?.hediffSet.HasHediff(HediffDef.Named("Merissu_SpecialGuestMarker")) == true)
            {
                Find.WindowStack.Add(new Dialog_MoonbowMarket(playerNegotiator, ___pawn));
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "ShouldShowQuestionMark")]
    public static class Patch_Pawn_ShowQuestionMark
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && __instance.health?.hediffSet.HasHediff(HediffDef.Named("Merissu_SpecialGuestMarker")) == true)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetFloatMenuOptions")]
    public static class Patch_Pawn_SpecialTradeOption
    {
        public static void Postfix(Pawn __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance.health?.hediffSet.HasHediff(HediffDef.Named("Merissu_SpecialGuestMarker")) != true) return;

            if (!selPawn.CanReach(__instance, PathEndMode.Touch, Danger.Deadly)) return;

            string label = "与卡牌商人交易";

            if (__result.Any(opt => opt.Label == label)) return;

            FloatMenuOption specialTradeOption = new FloatMenuOption(label, () =>
            {
                JobDef jobDef = DefDatabase<JobDef>.GetNamed("Merissu_GotoMoonbowMarket");

                Job tradeJob = JobMaker.MakeJob(jobDef, __instance);

                selPawn.jobs.TryTakeOrderedJob(tradeJob, JobTag.Misc);
            });
            __result = __result.Concat(new List<FloatMenuOption> { specialTradeOption });
        }
    }
}