using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.Sound;
using HarmonyLib;
using UnityEngine;

namespace merissu
{
    [RimWorld.DefOf]
    public static class PengLaiDefOf
    {
        public static HediffDef FabledAbodeImmortals;
        public static SongDef Mokou;

        static PengLaiDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PengLaiDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class PengLaiMod_Core
    {
        public static readonly AccessTools.FieldRef<PawnCapacitiesHandler, Pawn> HandlerPawnField =
            AccessTools.FieldRefAccess<PawnCapacitiesHandler, Pawn>("pawn");

        public static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> HealthTrackerPawnField =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        static PengLaiMod_Core()
        {
            Log.Message("月まで届け、不死の烟 - 蓬莱之药已初始化");
        }
    }

    public class Hediff_PengLaiMedicineDeadless : HediffWithComps
    {
        public bool isOriginal = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isOriginal, "isOriginal", false);
        }

        public override float Severity
        {
            get => base.Severity;
            set => base.Severity = Mathf.Max(value, 1.0f);
        }

        public override bool ShouldRemove => false;
    }


    [HarmonyPatch(typeof(PawnCapacitiesHandler), "GetLevel")]
    public static class Patch_Consciousness_Lock
    {
        public static void Postfix(PawnCapacitiesHandler __instance, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.Consciousness) return;

            Pawn pawn = PengLaiMod_Core.HandlerPawnField(__instance);
            if (pawn?.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(PengLaiDefOf.FabledAbodeImmortals))
            {
                __result = 1.0f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "RemoveHediff")]
    public static class Patch_Prevent_Hediff_Removal
    {
        public static bool Prefix(Hediff hediff)
        {
            return !(hediff is Hediff_PengLaiMedicineDeadless hp && hp.isOriginal);
        }
    }

    public static class PengLaiGuard
    {
        public static bool IsInIngestionProcess = false;
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_Restrict_Penglai_Addition
    {
        public static bool Prefix(Hediff hediff)
        {
            if (hediff.def == PengLaiDefOf.FabledAbodeImmortals && !PengLaiGuard.IsInIngestionProcess)
            {
                return false;
            }
            return true;
        }
    }

    public class IngestionOutcomeDoer_GivePenglaiImmortal : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            PengLaiGuard.IsInIngestionProcess = true;
            try
            {
                pawn.health.AddHediff(PengLaiDefOf.FabledAbodeImmortals);
                if (pawn.health.hediffSet.GetFirstHediffOfDef(PengLaiDefOf.FabledAbodeImmortals) is Hediff_PengLaiMedicineDeadless hed)
                {
                    hed.isOriginal = true;
                }
            }
            finally
            {
                PengLaiGuard.IsInIngestionProcess = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDead")]
    public static class Patch_Never_Dead
    {
        public static bool Prefix(Pawn_HealthTracker __instance, ref bool __result)
        {
            Pawn pawn = PengLaiMod_Core.HealthTrackerPawnField(__instance);
            if (pawn?.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(PengLaiDefOf.FabledAbodeImmortals))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public static class Patch_CheckCloneImmunity
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.health?.hediffSet == null) return;

            if (__instance.health.hediffSet.GetFirstHediffOfDef(PengLaiDefOf.FabledAbodeImmortals) is Hediff_PengLaiMedicineDeadless hed && !hed.isOriginal)
            {
                __instance.health.RemoveHediff(hed);
                Log.Warning($"{__instance.Name} 是克隆人，已移除蓬莱人体质。");
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Prevent_Kill
    {
        public static bool Prefix(Pawn __instance)
        {
            return !(__instance.health?.hediffSet?.HasHediff(PengLaiDefOf.FabledAbodeImmortals) ?? false);
        }
    }

    [HarmonyPatch(typeof(Pawn), "Destroy")]
    public static class Patch_Anti_Erasure
    {
        public static bool Prefix(Pawn __instance, DestroyMode mode)
        {
            if (__instance.Map != null && (__instance.health?.hediffSet?.HasHediff(PengLaiDefOf.FabledAbodeImmortals) ?? false))
            {
                if (mode == DestroyMode.KillFinalize || mode == DestroyMode.Vanish)
                {
                    EscapeDestruction(__instance);
                    return false;
                }
            }
            return true;
        }

        private static void EscapeDestruction(Pawn pawn)
        {
            Map map = pawn.Map;
            if (!CellFinder.TryFindRandomCell(map, c => c.Standable(map) && !c.Fogged(map), out IntVec3 safeLoc))
            {
                safeLoc = DropCellFinder.RandomDropSpot(map);
            }

            pawn.Position = safeLoc;
            pawn.Notify_Teleported();

            if (pawn.health.Dead || pawn.health.ShouldBeDeadFromRequiredCapacity() != null)
            {
                pawn.health.RestorePart(pawn.RaceProps.body.corePart);

                var hSet = pawn.health.hediffSet;
                hSet.hediffs.RemoveAll(h => h.def != PengLaiDefOf.FabledAbodeImmortals && (h is Hediff_Injury || h is Hediff_MissingPart));
            }

            Messages.Message("「不死鸟重生」", new TargetInfo(safeLoc, map), MessageTypeDefOf.PositiveEvent);
            PlayMokouBGM();
        }

        private static void PlayMokouBGM()
        {
            if (PengLaiDefOf.Mokou != null && Find.MusicManagerPlay != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(PengLaiDefOf.Mokou, true);
            }
        }
    }
}