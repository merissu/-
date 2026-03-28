using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class WorkGiver_RepairWithWanbao : WorkGiver_Repair
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Job vanillaJob = base.JobOnThing(pawn, t, forced);

            if (pawn?.equipment?.Primary?.def != MerissuDefOf.WanbaoHammer)
                return vanillaJob;

            Ability ability = pawn.abilities?.GetAbility(
                MerissuDefOf.Merissu_WanbaoHammerRepair,
                includeTemporary: true
            );

            if (ability == null) return vanillaJob;
            if (!ability.CanCast) return vanillaJob;
            if (!ability.CanApplyOn((LocalTargetInfo)t)) return vanillaJob;

            return ability.GetJob(t, LocalTargetInfo.Invalid);
        }
    }
}