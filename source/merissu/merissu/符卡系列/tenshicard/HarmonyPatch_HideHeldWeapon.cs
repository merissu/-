using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace merissu
{
    [HarmonyPatch]
    public static class Patch_UnequipWeaponOnSpellState
    {
        private const bool PutIntoInventory = true;

        private const bool PrimaryOnly = false;

        private static HediffDef hediffDef;
        private static bool busy;
        private static readonly List<ThingWithComps> tmpEquipment = new List<ThingWithComps>();

        private static HediffDef TargetDef
        {
            get
            {
                if (hediffDef == null)
                {
                    hediffDef = DefDatabase<HediffDef>.GetNamed("Hediff_TenshiLastSpell", errorOnFail: false);
                }
                return hediffDef;
            }
        }

        public static bool HasState(Pawn pawn)
        {
            HediffDef def = TargetDef;
            if (def == null || pawn == null)
            {
                return false;
            }
            HediffSet set = pawn.health?.hediffSet;
            return set != null && set.HasHediff(def);
        }

        [HarmonyPatch(typeof(HediffSet), "AddDirect",
            new Type[] { typeof(Hediff), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
        [HarmonyPostfix]
        public static void Postfix_AddDirect(HediffSet __instance, Hediff hediff)
        {
            if (busy || hediff == null || hediff.def != TargetDef)
            {
                return;
            }
            UnequipAll(__instance.pawn);
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
        [HarmonyPostfix]
        public static void Postfix_Notify_EquipmentAdded(Pawn_EquipmentTracker __instance)
        {
            if (busy || !HasState(__instance.pawn))
            {
                return;
            }
            UnequipAll(__instance.pawn);
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
        [HarmonyPostfix]
        public static void Postfix_SpawnSetup(Pawn __instance)
        {
            if (busy || !HasState(__instance))
            {
                return;
            }
            UnequipAll(__instance);
        }

        private static void UnequipAll(Pawn pawn)
        {
            if (busy || pawn?.equipment == null)
            {
                return;
            }

            Pawn_EquipmentTracker tracker = pawn.equipment;
            if (!tracker.HasAnything())
            {
                return;
            }

            ThingOwner container = PutIntoInventory ? pawn.inventory?.innerContainer : null;

            if (container == null && !pawn.Spawned)
            {
                return;
            }

            busy = true;
            try
            {
                tmpEquipment.Clear();
                tmpEquipment.AddRange(tracker.AllEquipmentListForReading);

                for (int i = 0; i < tmpEquipment.Count; i++)
                {
                    ThingWithComps eq = tmpEquipment[i];
                    if (eq == null || !tracker.Contains(eq))
                    {
                        continue;
                    }
                    if (PrimaryOnly && eq != tracker.Primary)
                    {
                        continue;
                    }
                    Unequip(pawn, tracker, eq, container);
                }
            }
            finally
            {
                tmpEquipment.Clear();
                busy = false;
            }
        }

        private static void Unequip(Pawn pawn, Pawn_EquipmentTracker tracker, ThingWithComps eq, ThingOwner container)
        {
            Job curJob = pawn.jobs?.curJob;
            if (curJob?.verbToUse != null && curJob.verbToUse.EquipmentSource == eq)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            if (container != null && tracker.TryTransferEquipmentToContainer(eq, container))
            {
                return;
            }

            if (pawn.Spawned && tracker.TryDropEquipment(eq, out _, pawn.Position, forbid: false))
            {
                return;
            }

            tracker.Remove(eq);
            Log.Warning($"[merissu] {pawn} 无法正常收回 {eq}，已直接从装备栏移除。");
        }
    }
}