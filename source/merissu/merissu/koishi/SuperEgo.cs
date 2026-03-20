using RimWorld;
using Verse;

namespace merissu
{
    public class SuperEgo : Ability
    {
        public SuperEgo() : base() { }

        public SuperEgo(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff superEgo = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TheLiberationOfTheId"));
            if (superEgo != null)
            {
                pawn.health.RemoveHediff(superEgo);
            }

            HediffDef hediffToAdd = HediffDef.Named("SuperEgo");
            if (hediffToAdd != null)
            {
                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);
                Messages.Message(pawn.LabelShort + " 认真了起来！", pawn, MessageTypeDefOf.PositiveEvent);
            }

            return base.Activate(target, dest);
        }
    }
}