using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class PengLaiMod_Core
    {
        static PengLaiMod_Core()
        {
            var harmony = new Harmony("com.merissu.penglai_immortal");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("月まで届け、不死の煙");
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
            set => base.Severity = UnityEngine.Mathf.Max(value, 1.0f);
        }

        public override bool ShouldRemove => false;
    }

    [HarmonyPatch(typeof(PawnCapacitiesHandler), "GetLevel")]
    public static class Patch_Consciousness_Lock
    {
        public static void Postfix(PawnCapacitiesHandler __instance, PawnCapacityDef capacity, ref float __result)
        {
            if (capacity != PawnCapacityDefOf.Consciousness) return;

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

            if (pawn != null && pawn.health.hediffSet.HasHediff(HediffDef.Named("FabledAbodeImmortals")))
            {
                __result = 1.0f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "RemoveHediff")]
    public static class Patch_Prevent_Hediff_Removal
    {
        public static bool Prefix(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff is Hediff_PengLaiMedicineDeadless hp)
            {
                if (hp.isOriginal)
                {
                    return false; 
                }
            }

            return true; 
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
            if (hediff.def.defName == "FabledAbodeImmortals" && !PengLaiGuard.IsInIngestionProcess)
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
            merissu.PengLaiGuard.IsInIngestionProcess = true;
            try
            {
                HediffDef immortalDef = HediffDef.Named("FabledAbodeImmortals");
                pawn.health.AddHediff(immortalDef);

                var hed = pawn.health.hediffSet.GetFirstHediffOfDef(immortalDef) as Hediff_PengLaiMedicineDeadless;
                if (hed != null)
                {
                    hed.isOriginal = true;
                }
            }
            finally
            {
                merissu.PengLaiGuard.IsInIngestionProcess = false;
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDead")]
    public static class Patch_Never_Dead
    {
        public static bool Prefix(Pawn_HealthTracker __instance, ref bool __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null && pawn.health.hediffSet.HasHediff(HediffDef.Named("FabledAbodeImmortals")))
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

            var hed = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FabledAbodeImmortals")) as Hediff_PengLaiMedicineDeadless;

            if (hed != null && !hed.isOriginal)
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
            if (__instance.health.hediffSet.HasHediff(HediffDef.Named("FabledAbodeImmortals")))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Destroy")]
    public static class Patch_Anti_Erasure
    {
        public static bool Prefix(Pawn __instance, DestroyMode mode)
        {
            if (__instance.Map != null &&
                __instance.health?.hediffSet != null &&
                __instance.health.hediffSet.HasHediff(HediffDef.Named("FabledAbodeImmortals")))
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

            if (!CellFinder.TryFindRandomCell(map, (IntVec3 c) =>
                c.Standable(map) &&
                !c.Fogged(map) &&
                !c.Roofed(map), out IntVec3 safeLoc))
            {
                safeLoc = DropCellFinder.RandomDropSpot(map);
            }

            pawn.Position = safeLoc;
            pawn.Notify_Teleported();

            if (pawn.health.Dead || pawn.health.ShouldBeDeadFromRequiredCapacity() != null)
            {
                pawn.health.RestorePart(pawn.RaceProps.body.corePart);

                var hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffs[i].def.defName != "FabledAbodeImmortals" &&
                       (hediffs[i] is Hediff_Injury || hediffs[i] is Hediff_MissingPart))
                    {
                        pawn.health.RemoveHediff(hediffs[i]);
                    }
                }
            }

            Messages.Message($"「不死鸟重生」",
                new TargetInfo(safeLoc, map), MessageTypeDefOf.PositiveEvent);

            PlayMokouBGM();

            Log.Message($"{pawn.Name}已重生");
        }

        private static void PlayMokouBGM()
        {
            SongDef song = DefDatabase<SongDef>.GetNamedSilentFail("Mokou");
            if (song == null) return;

            if (Find.MusicManagerPlay != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(song, true);
            }
        }
    }
}
