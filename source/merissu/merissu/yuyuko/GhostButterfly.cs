using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace merissu
{

    [StaticConstructorOnStartup]
public static class GhostButterfly_Main
{
    public static HediffDef GhostDef;

    public static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PawnField =
        AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");

        static GhostButterfly_Main()
        {
            GhostDef = DefDatabase<HediffDef>.GetNamed("Hediff_GhostButterfly", false);

            var harmony = new Harmony("merissu.ghostbutterfly");
            harmony.PatchAll();
        }

        public static bool IsGhost(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || GhostDef == null)
                return false;

            return pawn.health.hediffSet.HasHediff(GhostDef);
        }
    }

    [HarmonyPatch(typeof(Pawn), "get_Flying")]
    public static class Patch_Pawn_Flying
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(__instance))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower),
                  "CostToMoveIntoCell",
                  new Type[] { typeof(IntVec3) })]
    public static class Patch_Ghost_NoMoveCost
    {
        static bool Prefix(Pawn_PathFollower __instance, IntVec3 c, ref float __result)
        {
            Pawn pawn = GhostButterfly_Main.PawnField(__instance);

            if (GhostButterfly_Main.IsGhost(pawn))
            {
                __result = 1f;
                return false;
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(Pawn_PathFollower),
                  "WillCollideWithPawnAt",
                  new Type[] { typeof(IntVec3), typeof(bool), typeof(bool) })]
    public static class Patch_Ghost_NoPawnCollision
    {
        static bool Prefix(
            Pawn_PathFollower __instance,
            IntVec3 c,
            bool forceOnlyStanding,
            bool useId,
            ref bool __result)
        {
            Pawn pawn = GhostButterfly_Main.PawnField(__instance);

            if (GhostButterfly_Main.IsGhost(pawn))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GenGrid), "Walkable")]
    public static class Patch_Ghost_WalkThroughWalls
    {
        static void Postfix(IntVec3 c, Map map, ref bool __result)
        {
            if (__result)
                return;

            if (map == null)
                return;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Position == c && GhostButterfly_Main.IsGhost(pawn))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    public class HediffCompProperties_GhostSafety : HediffCompProperties
    {
        public HediffCompProperties_GhostSafety()
        {
            this.compClass = typeof(HediffComp_GhostSafety);
        }
    }

    public class HediffComp_GhostSafety : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            Pawn pawn = this.Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
                return;

            if (!pawn.Position.Walkable(pawn.Map))
            {
                IntVec3 safePos = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 6);

                if (safePos.IsValid && safePos != pawn.Position)
                {
                    pawn.Position = safePos;
                    pawn.Notify_Teleported();

                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        Messages.Message(
                            pawn.LabelShort + " 实体化在了安全位置。",
                            pawn,
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
        }
    }
}
