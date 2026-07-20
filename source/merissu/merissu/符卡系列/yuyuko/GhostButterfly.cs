using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class GhostButterfly_Main
    {
        public static HediffDef GhostDef;
        public static PathGridDef GhostFlyingGridDef;

        public static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");

        static GhostButterfly_Main()
        {
            GhostDef = DefDatabase<HediffDef>.GetNamed("Hediff_GhostButterfly", false);
            GhostFlyingGridDef = DefDatabase<PathGridDef>.GetNamed("GhostFlyingGrid", false);
        }

        public static bool IsGhost(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || GhostDef == null)
                return false;
            return pawn.health.hediffSet.HasHediff(GhostDef);
        }
    }

    public class GhostPathGrid : PathGrid
    {
        public GhostPathGrid(Map map, PathGridDef def) : base(map, def) { }

        public override int CalculatedCostAt(IntVec3 c, bool perceivedStatic, IntVec3 prevCell, int? baseCostOverride = null)
        {
            return 10;
        }
    }

    public static class StartPathGuard
    {
        [ThreadStatic] public static Pawn Pawn;
        [ThreadStatic] public static bool Active;
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "StartPath")]
    public static class Patch_Ghost_StartPath
    {
        static void Prefix(Pawn ___pawn)
        {
            if (GhostButterfly_Main.IsGhost(___pawn))
            {
                StartPathGuard.Active = true;
                StartPathGuard.Pawn = ___pawn;
            }
        }

        static void Postfix()
        {
            StartPathGuard.Active = false;
            StartPathGuard.Pawn = null;
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove), "GetSingleOption")]
    public static class Patch_Ghost_DraftedMoveMenu
    {
        static bool Prefix(FloatMenuContext context, ref FloatMenuOption __result)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (!GhostButterfly_Main.IsGhost(pawn))
                return true;

            if (!context.ClickedCell.IsValid)
                return true;

            if (!context.IsMultiselect)
            {
                if (context.ClickedCell == pawn.Position)
                    return true;

                __result = new FloatMenuOption(
                    "GoHere".Translate(),
                    delegate
                    {
                        FloatMenuOptionProvider_DraftedMove.PawnGotoAction(
                            context.ClickedCell, pawn, context.ClickedCell);
                    },
                    MenuOptionPriority.GoHere, null, null, 0f, null, null, true, 0)
                {
                    isGoto = true,
                    autoTakeable = true,
                    autoTakeablePriority = 10f
                };
                return false;
            }
            else
            {
                var tmpPawns = new List<Pawn>();
                foreach (Pawn p in context.ValidSelectedPawns)
                {
                    if (GhostButterfly_Main.IsGhost(p))
                        tmpPawns.Add(p);
                }
                if (tmpPawns.Count == 0)
                    return true;

                __result = new FloatMenuOption(
                    "GoHere".Translate(),
                    delegate
                    {
                        Find.Selector.gotoController.StartInteraction(context.ClickedCell);
                        foreach (Pawn p in tmpPawns)
                            Find.Selector.gotoController.AddPawn(p);
                        Find.Selector.gotoController.FinalizeInteraction();
                    },
                    MenuOptionPriority.GoHere, null, null, 0f, null, null, true, 0)
                {
                    isGoto = true,
                    autoTakeable = true,
                    autoTakeablePriority = 10f
                };
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetPathContext")]
    public static class Patch_Ghost_GetPathContext
    {
        public static void Postfix(Pawn __instance, Pathing pathing, ref PathingContext __result)
        {
            if (GhostButterfly_Main.IsGhost(__instance) && GhostButterfly_Main.GhostFlyingGridDef != null)
                __result = pathing.Get(GhostButterfly_Main.GhostFlyingGridDef);
        }
    }

    [HarmonyPatch(typeof(Pathing), "For", new Type[] { typeof(TraverseParms) })]
    public static class Patch_Ghost_For
    {
        public static void Postfix(Pathing __instance, ref PathingContext __result, TraverseParms parms)
        {
            if (GhostButterfly_Main.IsGhost(parms.pawn) && GhostButterfly_Main.GhostFlyingGridDef != null)
                __result = __instance.Get(GhostButterfly_Main.GhostFlyingGridDef);
        }
    }

    [HarmonyPatch(typeof(PathFinderMapData), "ParameterizeGridJob")]
    public static class Patch_Ghost_ParameterizeGridJob
    {
        public static void Postfix(PathFinderMapData __instance, PathRequest request, ref PathGridJob job, Map ___map)
        {
            if (GhostButterfly_Main.IsGhost(request.pawn) && GhostButterfly_Main.GhostFlyingGridDef != null)
            {
                var ctx = ___map.pathing.Get(GhostButterfly_Main.GhostFlyingGridDef);
                job.pathGridDirect = ctx.pathGrid.Grid_Unsafe.AsReadOnly();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Ghost_Tick_Fog
    {
        private static readonly Dictionary<Pawn, List<IntVec3>> unfogged = new Dictionary<Pawn, List<IntVec3>>();

        public static void Postfix(Pawn __instance)
        {
            if (!GhostButterfly_Main.IsGhost(__instance) || __instance.Map == null)
                return;

            if (__instance.IsHashIntervalTick(15))
            {
                if (unfogged.TryGetValue(__instance, out var list))
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var c = list[i];
                        if (c.DistanceTo(__instance.Position) > 8f)
                        {
                            __instance.Map.fogGrid.Refog(CellRect.FromCell(c));
                            list.RemoveAt(i);
                        }
                    }
                }

                foreach (var c in GenRadial.RadialCellsAround(__instance.Position, 5f, true))
                {
                    if (c.InBounds(__instance.Map) && c.Fogged(__instance.Map))
                    {
                        __instance.Map.fogGrid.Unfog(c);
                        if (!unfogged.TryGetValue(__instance, out var fogList))
                        {
                            fogList = new List<IntVec3>();
                            unfogged[__instance] = fogList;
                        }
                        fogList.Add(c);
                    }
                }
            }
        }

        public static void ClearFogFor(Pawn pawn)
        {
            if (pawn?.Map == null) return;
            if (unfogged.TryGetValue(pawn, out var list))
            {
                foreach (var c in list)
                    pawn.Map.fogGrid.Refog(CellRect.FromCell(c));
                unfogged.Remove(pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_Ghost_MakeDowned
    {
        public static bool Prefix(Pawn ___pawn)
        {
            if (GhostButterfly_Main.IsGhost(___pawn) && ___pawn.Spawned)
            {
                var map = ___pawn.Map;
                var pos = ___pawn.Position;
                var dest = CellFinder.StandableCellNear(pos, map, 24f, c => true);
                if (dest.IsValid)
                {
                    var flyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, ___pawn, dest, null, null, false, pos.ToVector3());
                    GenSpawn.Spawn(flyer, pos, map);
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class Patch_Ghost_Kill_FlyingCrash
    {
        public static void Prefix(Pawn __instance)
        {
            if (GhostButterfly_Main.IsGhost(__instance))
            {
                var hediff = __instance.health?.hediffSet?.GetFirstHediffOfDef(GhostButterfly_Main.GhostDef);
                if (hediff != null)
                    __instance.health.RemoveHediff(hediff);
            }
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

    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(IntVec3) })]
    public static class Patch_Ghost_NoMoveCost
    {
        static bool Prefix(Pawn_PathFollower __instance, IntVec3 c, ref float __result, Pawn ___pawn)
        {
            if (GhostButterfly_Main.IsGhost(___pawn))
            {
                __result = ___pawn.TicksPerMoveCardinal;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "WillCollideWithPawnAt", new Type[] { typeof(IntVec3), typeof(bool), typeof(bool) })]
    public static class Patch_Ghost_NoPawnCollision
    {
        static bool Prefix(Pawn_PathFollower __instance, IntVec3 c, bool forceOnlyStanding, bool useId, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(GhostButterfly_Main.PawnField(__instance)))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenGrid), nameof(GenGrid.StandableBy))]
    public static class Patch_Ghost_StandableBy
    {
        public static void Postfix(IntVec3 c, Map map, Pawn pawn, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(pawn))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "BuildingBlockingNextPathCell")]
    public static class Patch_Ghost_BuildingBlockingNextPathCell
    {
        public static void Postfix(Pawn ___pawn, ref Building __result)
        {
            if (GhostButterfly_Main.IsGhost(___pawn))
                __result = null;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "NextCellDoorToWaitForOrManuallyOpen")]
    public static class Patch_Ghost_NextCellDoorToWaitForOrManuallyOpen
    {
        public static void Postfix(Pawn ___pawn, ref Building_Door __result)
        {
            if (GhostButterfly_Main.IsGhost(___pawn))
                __result = null;
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "PawnCanOccupy")]
    public static class Patch_Ghost_PawnCanOccupy
    {
        public static void Postfix(Pawn ___pawn, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(___pawn))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach),
        new Type[] { typeof(Pawn), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(Danger), typeof(bool), typeof(bool), typeof(TraverseMode) })]
    public static class Patch_Ghost_CanReach_1
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(pawn))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach),
        new Type[] { typeof(Pawn), typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(Danger), typeof(bool), typeof(bool), typeof(TraverseMode) })]
    public static class Patch_Ghost_CanReach_2
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (GhostButterfly_Main.IsGhost(pawn))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Reachability), "CanReach",
        new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) })]
    public static class Patch_Ghost_Reachability_CanReach
    {
        static bool Prefix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result)
        {
            if (traverseParams.pawn != null && GhostButterfly_Main.IsGhost(traverseParams.pawn))
            {
                __result = true;
                return false;
            }

            if (StartPathGuard.Active && StartPathGuard.Pawn != null)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(JobGiver_MoveToStandable), "TryGiveJob")]
    public static class Patch_Ghost_MoveToStandable
    {
        static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (GhostButterfly_Main.IsGhost(pawn))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
    public static class Patch_Ghost_ThreatDisabled
    {
        public static void Postfix(Pawn __instance, IAttackTargetSearcher disabledFor, ref bool __result)
        {
            if (__result) return;
            if (!GhostButterfly_Main.IsGhost(__instance)) return;
            if (disabledFor?.Thing is Pawn attacker && GhostButterfly_Main.IsGhost(attacker)) return;
            __result = true;
        }
    }

    public class HediffCompProperties_GhostSafety : HediffCompProperties
    {
        public HediffCompProperties_GhostSafety()
        {
            compClass = typeof(HediffComp_GhostSafety);
        }
    }

    public class HediffComp_GhostSafety : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn pawn = this.Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            Patch_Ghost_Tick_Fog.ClearFogFor(pawn);

            if (!pawn.Position.Walkable(pawn.Map))
            {
                Map map = pawn.Map;
                IntVec3 safePos = FindSafePosition(pawn);
                if (safePos.IsValid)
                {
                    pawn.Position = safePos;
                    pawn.Notify_Teleported();
                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                        Messages.Message(pawn.LabelShort + " 实体化在了安全位置。", pawn, MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    Log.Error($"GhostButterfly: Could not find safe position for {pawn.LabelShort} after hediff removal. Killing pawn.");
                    pawn.Kill(null);
                }
            }
        }
        private IntVec3 FindSafePosition(Pawn pawn)
        {
            Map map = pawn.Map;
            IntVec3 current = pawn.Position;

            for (int radius = 3; radius <= 20; radius += 2)
            {
                if (CellFinder.TryRandomClosewalkCellNear(current, map, radius, out var pos))
                {
                    if (pos != current)
                        return pos;
                }
            }

            if (CellFinder.TryFindRandomCell(map, (IntVec3 c) => c.Walkable(map) && c != current, out var result))
                return result;

            if (DropCellFinder.TryFindDropSpotNear(current, map, out var dropSpot, false, false))
                return dropSpot;

            return IntVec3.Invalid;
        }
    }
}