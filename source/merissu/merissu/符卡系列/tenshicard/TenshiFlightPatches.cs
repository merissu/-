using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public static class TenshiFlightUtility
    {
        private static HediffDef cachedDef;

        public static HediffDef TenshiLastSpellDef
        {
            get
            {
                if (cachedDef == null)
                    cachedDef = DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_TenshiLastSpell");
                return cachedDef;
            }
        }

        public static bool IsTenshiFlying(Pawn pawn)
        {
            return pawn != null
                   && pawn.health != null
                   && TenshiLastSpellDef != null
                   && pawn.health.hediffSet.HasHediff(TenshiLastSpellDef);
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class Patch_Pawn_PathFollower_StartPath
    {
        public static bool Prefix(Pawn ___pawn)
        {
            return !TenshiFlightUtility.IsTenshiFlying(___pawn);
        }
    }

    [HarmonyPatch(typeof(Pawn_FlightTracker), nameof(Pawn_FlightTracker.Notify_JobStarted))]
    public static class Patch_Pawn_FlightTracker_Notify_JobStarted
    {
        public static bool Prefix(Pawn ___pawn, Job job)
        {
            if (!TenshiFlightUtility.IsTenshiFlying(___pawn))
                return true;

            job.flying = true;
            return false;
        }
    }

    [StaticConstructorOnStartup]
    public static class TenshiFlightHarmony
    {
        static TenshiFlightHarmony()
        {
            new Harmony("merissu.tenshilastspell").PatchAll();
        }
    }
}