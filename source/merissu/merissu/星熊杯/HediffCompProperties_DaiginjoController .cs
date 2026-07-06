using HarmonyLib;
using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_DaiginjoController : HediffCompProperties
    {
        public HediffCompProperties_DaiginjoController()
        {
            compClass = typeof(HediffComp_DaiginjoController);
        }
    }

    public class HediffComp_DaiginjoController : HediffComp
    {
        private int ticksUntilCleanup;
        private const int CleanupInterval = 250;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RemoveNonAlcoholAddictions();
            ticksUntilCleanup = CleanupInterval;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksUntilCleanup--;
            if (ticksUntilCleanup <= 0)
            {
                ticksUntilCleanup = CleanupInterval;
                RemoveNonAlcoholAddictions();
            }
        }

        private void RemoveNonAlcoholAddictions()
        {
            if (Pawn?.health == null || Pawn.Dead) return;

            var hediffs = Pawn.health.hediffSet.hediffs;
            var alcoholAddictionHediff = ChemicalDefOf.Alcohol?.addictionHediff;

            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i] is Hediff_Addiction addiction &&
                    addiction.def != alcoholAddictionHediff)
                {
                    Pawn.health.RemoveHediff(addiction);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Hediff_Alcohol), nameof(Hediff_Alcohol.TickInterval))]
    public static class Patch_DaiginjoNoHangover
    {
        public static bool Prefix(Hediff_Alcohol __instance)
        {
            if (__instance.def.defName == "Daiginjoalcohol")
            {
                return false;
            }
            return true;
        }
    }
}
