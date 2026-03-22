using HarmonyLib;
using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_NoHitStagger : HediffCompProperties
    {
        public HediffCompProperties_NoHitStagger()
        {
            compClass = typeof(HediffComp_NoHitStagger);
        }
    }

    [HarmonyPatch(typeof(PawnCapacityUtility), nameof(PawnCapacityUtility.CalculateCapacityLevel))]
    public static class Patch_PawnCapacityUtility_CalculateCapacityLevel
    {
        static void Postfix(HediffSet diffSet, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.Consciousness) return;
            if (diffSet?.pawn == null) return;
            if (!DragonStarUtility.HasNoHitStagger(diffSet.pawn)) return;

            __result = 1.0f;
        }
    }

    public class HediffComp_NoHitStagger : HediffComp
    {
    }

    [StaticConstructorOnStartup]
    public static class HarmonyInit_NoHitStagger
    {
        static HarmonyInit_NoHitStagger()
        {
            new Harmony("merissu.dragonstar.nohitstagger").PatchAll();
        }
    }

    public static class DragonStarUtility
    {
        public static bool HasNoHitStagger(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;

            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].TryGetComp<HediffComp_NoHitStagger>() != null)
                    return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(StaggerHandler), nameof(StaggerHandler.StaggerFor))]
    public static class Patch_StaggerHandler_StaggerFor
    {
        static bool Prefix(StaggerHandler __instance, ref bool __result)
        {
            if (DragonStarUtility.HasNoHitStagger(__instance.parent))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
    public static class Patch_StunHandler_Notify_DamageApplied
    {
        static bool Prefix(StunHandler __instance)
        {
            if (__instance.parent is Pawn pawn && DragonStarUtility.HasNoHitStagger(pawn))
            {
                return false;
            }
            return true;
        }
    }
}