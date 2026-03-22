using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace merissu
{
    public class HediffCompProperties_HeavenlyRainbow : HediffCompProperties
    {
        public float rainbowSpeed = 1.0f;
        public float saturation = 1.0f;
        public float brightness = 1.2f;

        public HediffCompProperties_HeavenlyRainbow()
        {
            this.compClass = typeof(HediffComp_HeavenlyRainbow);
        }
    }

    public class HediffComp_HeavenlyRainbow : HediffComp
    {
        public HediffCompProperties_HeavenlyRainbow Props => (HediffCompProperties_HeavenlyRainbow)props;
    }

    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_HeavenlyRainbowEffect
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnFieldRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
        {
            Pawn pawn = PawnFieldRef(__instance);
            if (pawn?.health?.hediffSet?.hediffs == null) return;

            HediffComp_HeavenlyRainbow comp = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                comp = hediffs[i].TryGetComp<HediffComp_HeavenlyRainbow>();
                if (comp != null) break;
            }

            if (comp == null || comp.parent.Severity < 3.0f) return;

            var p = comp.Props;

            float t = (Find.TickManager.TicksGame + pawn.thingIDNumber * 13) * (p.rainbowSpeed * 0.02f);
            float hue = t % 1.0f;

            Color rainbow = Color.HSVToRGB(hue, p.saturation, p.brightness);
            rainbow.a = __result.tint.a;

            __result.tint = rainbow;
        }
    }
}