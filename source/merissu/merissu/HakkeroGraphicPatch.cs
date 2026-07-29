using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class HakkeroGraphicPatch
    {
        private static Graphic graphicReady;
        private static Graphic graphicFiring;

        static HakkeroGraphicPatch()
        {
            var harmony = new Harmony("merissu.hakkero.graphics");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.Graphic), MethodType.Getter)]
        public static class Patch_HakkeroGraphic
        {
            public static void Postfix(Thing __instance, ref Graphic __result)
            {
                if ((__instance.def.defName == "Hakkero" || __instance.def.defName == "HakkeroLaser")
                    && __instance.ParentHolder is Pawn_EquipmentTracker eq)
                {
                    Pawn pawn = eq.pawn;
                    if (pawn == null) return;

                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("MasterSpark")) ||
                        pawn.health.hediffSet.HasHediff(HediffDef.Named("FinalMasterSpark")))
                    {
                        if (graphicFiring == null)
                            graphicFiring = GraphicDatabase.Get<Graphic_Single>(
                                "Weapons/firingHakkero",
                                __instance.def.graphic.Shader,
                                __instance.def.graphic.drawSize,
                                __instance.def.graphic.Color);
                        __result = graphicFiring;
                        return;
                    }

                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("MarisaCardDeclared")))
                    {
                        if (graphicReady == null)
                            graphicReady = GraphicDatabase.Get<Graphic_Single>(
                                "Weapons/readyHakkero",
                                __instance.def.graphic.Shader,
                                __instance.def.graphic.drawSize,
                                __instance.def.graphic.Color);
                        __result = graphicReady;
                        return;
                    }
                }
            }
        }
    }
}