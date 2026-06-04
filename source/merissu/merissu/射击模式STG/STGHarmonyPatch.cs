using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    public static class Pawn_RotationTracker_UpdateRotation_Patch
    {
        public static bool Prefix(Pawn_RotationTracker __instance)
        {
            if (!__instance.pawn.IsPC()) return true;

            if (__instance.pawn.stances.curStance is Stance_Busy)
            {
                return true;
            }

            bool doingJob = __instance.pawn.jobs?.curJob != null &&
                            __instance.pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait &&
                            __instance.pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait_Combat;

            if (doingJob)
            {
                return true;
            }

            if (State.PC?.IsMoving == true)
            {
                return false;
            }

            if (__instance.pawn.Drafted && !__instance.pawn.InMentalState && !State.ControlsFrozen)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PawnTweener), "get_TweenedPos")]
    public static class PawnTweener_get_TweenedPos_Patch
    {
        public static bool Prefix(PawnTweener __instance, ref Vector3 __result)
        {
            if (__instance.pawn.IsPC() && State.PC?.physicsPosition != null)
            {
                __result = State.PC.physicsPosition.Value;
                return false; 
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Update))]
    public static class Root_Play_Update_Patch
    {
        public static void Postfix()
        {
            State.Update();
        }
    }
    [HarmonyPatch(typeof(GenScene), nameof(GenScene.GoToMainMenu))]
    public static class Patch_GenScene_GoToMainMenu
    {
        public static void Prefix()
        {
            ManualControlManager.ForceReset();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class Game_InitNewGame_Patch
    {
        public static void Prefix()
        {
            ManualControlManager.ForceReset();
        }
    }
}