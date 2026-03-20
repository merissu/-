using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_ReimuCard : Ability
    {
        private const float FullPowerCost = 1f;
        public Ability_ReimuCard() : base() { }

        public Ability_ReimuCard(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted)
                    return baseReport;

                Hediff fullPower = pawn.health.hediffSet
                    .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (fullPower == null || fullPower.Severity < FullPowerCost)
                    return "需要至少一层灵力";

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fullPower = pawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (fullPower == null || fullPower.Severity < FullPowerCost)
                return false;

            fullPower.Severity -= FullPowerCost;
            if (fullPower.Severity <= 0f)
            {
                pawn.health.RemoveHediff(fullPower);
            }

            HediffDef declareDef = HediffDef.Named("ReimuCardDeclared");
            if (!pawn.health.hediffSet.HasHediff(declareDef))
            {
                pawn.health.AddHediff(declareDef);
            }

            return base.Activate(target, dest);
        }
    }
}
