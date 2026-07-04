using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public static class UniversalDrugDefs
    {
        public static readonly HashSet<string> DefNames = new HashSet<string>
        {
            "suikagourd",
            "OniKillerSake",
        };
    }

    [HarmonyPatch(typeof(JobGiver_SatisfyChemicalNeed))]
    [HarmonyPatch("DrugValidator")]
    public static class Patch_SatisfyChemicalNeed_DrugValidator
    {
        public static bool Prefix(ref bool __result, Pawn pawn, Hediff_Addiction addiction, Thing drug)
        {
            if (!UniversalDrugDefs.DefNames.Contains(drug.def.defName))
                return true;

            if (!drug.def.IsDrug)
            {
                __result = false;
                return false;
            }
            if (drug.Spawned && (!pawn.CanReserve(drug) || drug.IsForbidden(pawn) || !drug.IsSociallyProper(pawn) || !drug.IngestibleNow))
            {
                __result = false;
                return false;
            }
            CompDrug compDrug = drug.TryGetComp<CompDrug>();
            if (compDrug?.Props.chemical == null)
            {
                __result = false;
                return false;
            }

            DrugPolicy drugPolicy = pawn.drugs?.CurrentPolicy;
            if (drugPolicy != null && !drugPolicy[drug.def].allowedForAddiction
                && pawn.story != null
                && pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) <= 0
                && (!pawn.InMentalState || !pawn.MentalStateDef.ignoreDrugPolicy))
            {
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_SatifyChemicalDependency))]
    [HarmonyPatch("DrugValidator")]
    public static class Patch_SatifyChemicalDependency_DrugValidator
    {
        public static bool Prefix(ref bool __result, Pawn pawn, Hediff_ChemicalDependency dependency, Thing drug)
        {
            if (!UniversalDrugDefs.DefNames.Contains(drug.def.defName))
                return true;

            if (!drug.def.IsDrug)
            {
                __result = false;
                return false;
            }
            if (drug.Spawned && (!pawn.CanReserve(drug) || drug.IsForbidden(pawn) || !drug.IsSociallyProper(pawn) || !drug.IngestibleNow))
            {
                __result = false;
                return false;
            }
            CompDrug compDrug = drug.TryGetComp<CompDrug>();
            if (compDrug == null || compDrug.Props.chemical == null)
            {
                __result = false;
                return false;
            }

            // 跳过 chemical 匹配
            if (pawn.drugs != null && !pawn.drugs.CurrentPolicy[drug.def].allowedForAddiction
                && (!pawn.InMentalState || pawn.MentalStateDef.ignoreDrugPolicy))
            {
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }
}
