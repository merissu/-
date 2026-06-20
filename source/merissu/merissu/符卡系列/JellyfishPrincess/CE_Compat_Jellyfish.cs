using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class CE_Compat_Jellyfish
    {
        static CE_Compat_Jellyfish()
        {
            if (ModLister.HasActiveModWithName("Combat Extended"))
            {
                Log.Message("检测到 Combat Extended，CE 兼容补丁...");
                var harmony = new Harmony("merissu.jellyfish.ce_compat");
                PatchCE(harmony);
            }
        }

        private static void PatchCE(Harmony harmony)
        {
            var typeCompSuppressable = Type.GetType("CombatExtended.CompSuppressable, CombatExtended");
            if (typeCompSuppressable != null)
            {
                var addSuppressionMethod = AccessTools.Method(typeCompSuppressable, "AddSuppression");
                if (addSuppressionMethod != null)
                {
                    harmony.Patch(addSuppressionMethod,
                        prefix: new HarmonyMethod(typeof(CE_Compat_Jellyfish), nameof(Prefix_AddSuppression)));
                }
            }
        }

        public static bool Prefix_AddSuppression(ThingComp __instance)
        {
            if (__instance.parent is Pawn pawn)
            {
                if (pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("JellyfishPrincess")) != null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}