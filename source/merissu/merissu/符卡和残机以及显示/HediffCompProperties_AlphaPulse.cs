using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_AlphaPulse : HediffCompProperties
    {
        public float pulseFrequency = 0.12f; 
        public float minAlpha = 0.3f;        
        public float maxAlpha = 1.0f;        

        public HediffCompProperties_AlphaPulse()
        {
            compClass = typeof(HediffComp_AlphaPulse);
        }
    }

    public class HediffComp_AlphaPulse : HediffComp
    {
        public HediffCompProperties_AlphaPulse Props => (HediffCompProperties_AlphaPulse)props;
    }

    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_AlphaPulse
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnFieldRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
        {
            Pawn pawn = PawnFieldRef(__instance);
            if (pawn?.health?.hediffSet?.hediffs == null) return;

            HediffComp_AlphaPulse pulseComp = null;
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                pulseComp = hediffs[i].TryGetComp<HediffComp_AlphaPulse>();
                if (pulseComp != null) break;
            }

            if (pulseComp == null) return;

            var p = pulseComp.Props;

            float time = Find.TickManager.TicksGame + pawn.thingIDNumber * 37;

            float lerp = (Mathf.Sin(time * p.pulseFrequency) + 1f) / 2f;

            float currentAlpha = Mathf.Lerp(p.minAlpha, p.maxAlpha, lerp);

            Color finalColor = __result.tint;
            finalColor.a *= currentAlpha; 
            __result.tint = finalColor;
        }
    }
}