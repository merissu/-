using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public static class FlightHeightUtility
    {
        public const float VanillaHeight = 0.6f;

        public const bool DebugHeight = false;

        public static float GetHeight(Pawn pawn)
        {
            if (pawn?.health == null || pawn.flight == null || !pawn.flight.Flying)
                return 0f;

            HediffComp_TenshiFlight comp = GetTenshiComp(pawn);
            if (comp == null)
                return 0f;

            float height = Mathf.Clamp(comp.Props.flightHeightFactor, 0f, 20f);

            if (DebugHeight)
            {
                Log.Message($"[FlightHeight] {pawn} flying={pawn.flight.Flying} " +
                            $"factor={pawn.flight.PositionOffsetFactor} height={height}");
            }

            return height;
        }

        public static HediffComp_TenshiFlight GetTenshiComp(Pawn pawn)
        {
            if (pawn?.health == null)
                return null;

            HediffDef def = TenshiFlightUtility.TenshiLastSpellDef;
            if (def == null)
                return null;

            return pawn.health.hediffSet
                       .GetFirstHediffOfDef(def)?
                       .TryGetComp<HediffComp_TenshiFlight>();
        }

        public static Material GetShadowMaterial(float opacity)
        {
            return MaterialPool.MatFrom(
                "Things/Skyfaller/SkyfallerShadowCircle",
                ShaderDatabase.Transparent,
                new Color(1f, 1f, 1f, Mathf.Clamp(opacity, 0f, 1f)));
        }
    }

    [HarmonyPatch(typeof(Pawn_DrawTracker), "FlyingOffset")]
    public static class Patch_Pawn_DrawTracker_FlyingOffset
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn ___pawn, ref Vector3 __result)
        {
            float height = FlightHeightUtility.GetHeight(___pawn);
            if (height <= 0f)
                return;

            __result = new Vector3(0f, 0f, height * ___pawn.flight.PositionOffsetFactor);
        }
    }

    [HarmonyPatch(typeof(Pawn_DrawTracker), "FlightYOffset")]
    public static class Patch_Pawn_DrawTracker_FlightYOffset
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn ___pawn, ref float __result)
        {
            float height = FlightHeightUtility.GetHeight(___pawn);
            if (height <= 0f)
                return;

            __result = 0.03658537f * (height / FlightHeightUtility.VanillaHeight)
                       * ___pawn.flight.PositionOffsetFactor;
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "DrawShadowInternal")]
    public static class Patch_PawnRenderer_DrawShadowInternal
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn ___pawn)
        {
            HediffComp_TenshiFlight comp = FlightHeightUtility.GetTenshiComp(___pawn);
            if (comp == null || ___pawn?.flight == null || !___pawn.flight.Flying)
                return true;

            float height = Mathf.Clamp(comp.Props.flightHeightFactor, 0f, 20f);
            if (height <= 0f)
                return true;

            float factor = ___pawn.flight.PositionOffsetFactor;

            Vector3 drawPos = ___pawn.DrawPos;
            drawPos.y = AltitudeLayer.Filth.AltitudeFor();
            drawPos -= new Vector3(0f, 0f, height * factor);

            float scale = Mathf.Clamp(comp.Props.flightShadowScale, 0.1f, 3f);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one * scale);
            Material material = FlightHeightUtility.GetShadowMaterial(comp.Props.flightShadowOpacity);

            int passes = Mathf.Clamp(comp.Props.flightShadowPasses, 1, 4);
            for (int i = 0; i < passes; i++)
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);

            return false;
        }
    }
}