using Verse;
using RimWorld;

namespace merissu
{
    public class Ability_AbyssNova : Ability
    {
        private const string POWER_HEDIFF_NAME = "FullPower";
        private const float COST_SEVERITY = 5f;
        public Ability_AbyssNova() : base() { }

        public Ability_AbyssNova(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(POWER_HEDIFF_NAME));

                if (hp == null || hp.Severity < COST_SEVERITY)
                {
                    return "灵力不足 (需要" + COST_SEVERITY + "层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(
            LocalTargetInfo target,
            LocalTargetInfo dest
        )
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(POWER_HEDIFF_NAME));

            if (hp == null || hp.Severity < COST_SEVERITY)
            {
                Messages.Message("灵力不足", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!base.Activate(target, dest))
                return false;

            hp.Severity -= COST_SEVERITY;

            AbyssNovaUIController.Start(5f, this.pawn);

            return true;
        }
    }
}