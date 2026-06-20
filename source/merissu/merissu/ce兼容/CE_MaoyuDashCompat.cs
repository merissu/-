using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class CE_MaoyuDashCompat
    {
        static CE_MaoyuDashCompat()
        {
            if (!ModsConfig.IsActive("CETeam.CombatExtended"))
                return;

            try
            {

                var targetMethod = AccessTools.Method(
                    typeof(CompAbilityEffect_MaoyuDashHit),
                    nameof(CompAbilityEffect_MaoyuDashHit.Apply),
                    new Type[]
                    {
                        typeof(LocalTargetInfo),
                        typeof(LocalTargetInfo)
                    });

                if (targetMethod == null)
                {
                    return;
                }

                new Harmony("merissu.ce.maoyudash").Patch(
                    targetMethod,
                    prefix: new HarmonyMethod(
                        typeof(CE_MaoyuDashCompat),
                        nameof(Prefix_Apply)));

            }
            catch (Exception ex)
            {
            }
        }

        public static bool Prefix_Apply(
            CompAbilityEffect_MaoyuDashHit __instance,
            LocalTargetInfo target,
            LocalTargetInfo dest)
        {
            Pawn caster = __instance.parent?.pawn;
            Pawn victim = target.Pawn;

            if (caster == null || victim == null)
                return false;

            if (!caster.Spawned || caster.Dead)
                return false;

            if (!victim.Spawned || victim.Dead)
                return false;

            try
            {
                Verb verb = caster.meleeVerbs?.TryGetMeleeVerb(victim);

                if (verb != null)
                {

                    verb.TryStartCastOn(victim);
                }
                else
                {
                    Log.Warning(
                        $"[Merissu CE] No melee verb found for {caster.Label}");

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Merissu CE] Error during melee attack:\n" + ex);

                return true;
            }

            __instance.RemoveDashSpeedHediff(caster);

            int bounceDist =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        __instance.parent.def.verbProperties?.range * 0.5f ?? 2f));

            Vector3 dir =
                (caster.Position - victim.Position).ToVector3();

            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.right;

            dir.Normalize();

            try
            {
                var bounceMethod = AccessTools.Method(
                    typeof(CompAbilityEffect_MaoyuDashHit),
                    "TryFindBounceCell",
                    new Type[]
                    {
                        typeof(Pawn),
                        typeof(Map),
                        typeof(Vector3),
                        typeof(int),
                        typeof(IntVec3).MakeByRefType()
                    });

                if (bounceMethod != null)
                {
                    object[] args =
                    {
                        caster,
                        caster.Map,
                        dir,
                        bounceDist,
                        IntVec3.Invalid
                    };

                    bool found =
                        (bool)bounceMethod.Invoke(__instance, args);

                    if (found)
                    {
                        IntVec3 bounceCell = (IntVec3)args[4];

                        if (caster.jobs?.curJob != null &&
                            caster.jobs.curJob.def.driverClass ==
                            typeof(JobDriver_CastAbilityDash))
                        {
                            caster.jobs.EndCurrentJob(
                                JobCondition.Succeeded,
                                false);
                        }

                        JumpUtility.DoJump(
                            caster,
                            new LocalTargetInfo(bounceCell),
                            null,
                            __instance.parent.def.verbProperties);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Merissu CE] Error during bounce:\n" + ex);
            }

            return false;
        }
    }
}