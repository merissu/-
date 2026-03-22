using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class GoldenPulse_Bootstrap
    {
        static GoldenPulse_Bootstrap()
        {
            var h = new Harmony("merissu.tengufan.goldenpulse");
            h.PatchAll();
        }
    }

    public class HediffCompProperties_RainbowCycle : HediffCompProperties
    {
        public float pulseFrequency = 0.1f;    
        public float pulseAmplitude = 1.0f;    
        public float brightnessBoost = 1.2f;   

        public HediffCompProperties_RainbowCycle()
        {
            compClass = typeof(HediffComp_RainbowCycle);
        }
    }

    public class HediffComp_RainbowCycle : HediffComp
    {
        public HediffCompProperties_RainbowCycle Props => (HediffCompProperties_RainbowCycle)props;
    }

    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_GetDrawParms_GoldenPulse
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnFieldRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        private static readonly Color GoldColor = new Color(1.0f, 0.84f, 0.0f);

        static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
        {
            Pawn pawn = PawnFieldRef(__instance);
            if (pawn?.health?.hediffSet?.hediffs == null) return;

            HediffComp_RainbowCycle goldenComp = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                goldenComp = hediffs[i].TryGetComp<HediffComp_RainbowCycle>();
                if (goldenComp != null) break;
            }

            if (goldenComp == null) return;

            var p = goldenComp.Props;
            int t = Find.TickManager.TicksGame + pawn.thingIDNumber * 37;

            float lerpFactor = 0.5f + 0.5f * Mathf.Sin(t * p.pulseFrequency);

            lerpFactor *= Mathf.Clamp01(p.pulseAmplitude);

            Color originalTint = __result.tint;
            float originalAlpha = originalTint.a;

            Color targetGold = GoldColor * p.brightnessBoost;
            Color finalColor = Color.Lerp(originalTint, targetGold, lerpFactor);

            finalColor.a = originalAlpha;
            __result.tint = finalColor;
        }
    }
}