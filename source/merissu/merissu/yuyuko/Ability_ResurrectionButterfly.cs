using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_ResurrectionButterfly : Ability
    {
        public Ability_ResurrectionButterfly() : base() { }
        public Ability_ResurrectionButterfly(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足"; 
                }

                return AcceptanceReport.WasAccepted; 
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            HediffDef hediffToAdd = HediffDef.Named("ResurrectionButterfly");
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hediffToAdd != null && hp != null)
            {
                hp.Severity -= 1f;

                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);

                Messages.Message(pawn.LabelShort + " 发动了反魂蝶！", pawn, MessageTypeDefOf.PositiveEvent);
            }

            return base.Activate(target, dest);
        }
    }
}