using HarmonyLib;
using RimWorld;
using System;
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
    [HarmonyPatch(typeof(Projectile), "CanHit")]
    public static class Patch_Projectile_CanHit
    {
        public static void Postfix(Projectile __instance, Thing thing, ref bool __result)
        {
            if (__result && State.IsActive && State.PC?.pawn != null && thing == State.PC.pawn)
            {
                __result = false;
            }
        }
    }
    [HarmonyPatch(typeof(Projectile), "Tick")]
    public static class Patch_Projectile_Tick_STG
    {
        private static readonly Action<Projectile, Thing, bool> CachedImpactAction =
            AccessTools.MethodDelegate<Action<Projectile, Thing, bool>>(AccessTools.Method(typeof(Projectile), "Impact"));

        public static void Prefix(Projectile __instance, out Vector3 __state)
        {
            __state = __instance.ExactPosition;
        }

        public static void Postfix(Projectile __instance, Vector3 __state)
        {
            if (!State.IsActive || State.PC?.pawn == null || __instance.Destroyed) return;

            Vector3 oldPos = __state;
            Vector3 newPos = __instance.ExactPosition;
            Vector3 centerPos = State.PC.physicsPosition ?? State.PC.pawn.DrawPos;

            if (STG_HitManager.SegmentIntersectsHitbox(oldPos, newPos, centerPos, STG_HitManager.HitboxHalfWidth))
            {
                ForceImpact(__instance, State.PC.pawn);
                return; 
            }

            if (STG_HitManager.SegmentIntersectsHitbox(oldPos, newPos, centerPos, STG_HitManager.GrazeHalfWidth))
            {
                State.PC.TryTriggerGraze(__instance);
            }
        }
        private static void ForceImpact(Projectile proj, Pawn target)
        {
            STG_HitManager.IsForcingHit = true;
            try
            {
                if (CachedImpactAction != null)
                {
                    CachedImpactAction(proj, target, false);
                }
                else
                {
                    Log.Message("失败");
                }
            }
            finally
            {
                STG_HitManager.IsForcingHit = false;
            }
        }
    }
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_Thing_TakeDamage_STG
    {
        public static bool Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            if (!State.IsActive || State.PC?.pawn == null || __instance != State.PC.pawn)
                return true;

            if (STG_HitManager.IsForcingHit)
                return true;

            if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
            {
                __result = new DamageWorker.DamageResult();
                return false;
            }

            return true;
        }
    }
    [HarmonyPatch(typeof(Projectile), "Launch", new Type[] {
        typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo),
        typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef)
    })]
    public static class Patch_Projectile_Launch_STG
    {
        private static AccessTools.FieldRef<Projectile, Vector3> destinationRef = AccessTools.FieldRefAccess<Projectile, Vector3>("destination");
        private static AccessTools.FieldRef<Projectile, int> ticksToImpactRef = AccessTools.FieldRefAccess<Projectile, int>("ticksToImpact");
        private static AccessTools.FieldRef<Projectile, int> lifetimeRef = AccessTools.FieldRefAccess<Projectile, int>("lifetime");

        public static void Postfix(Projectile __instance, Vector3 origin, LocalTargetInfo intendedTarget)
        {
            if (!State.IsActive || State.PC?.pawn == null || intendedTarget.Thing != State.PC.pawn)
                return;

            Vector3 preciseTarget = State.PC.physicsPosition ?? State.PC.pawn.DrawPos;

            Vector3 dir = (preciseTarget - origin).Yto0().normalized;

            if (dir.sqrMagnitude < 0.001f) return;

            Vector3 newDestination = origin + dir * 60f;

            float speed = __instance.def.projectile.SpeedTilesPerTick;
            if (speed <= 0f) speed = 0.001f;
            float dist = (origin - newDestination).magnitude;
            int newTicks = Mathf.CeilToInt(dist / speed);
            if (newTicks < 1) newTicks = 1;

            destinationRef(__instance) = newDestination;
            ticksToImpactRef(__instance) = newTicks;
            lifetimeRef(__instance) = newTicks;
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