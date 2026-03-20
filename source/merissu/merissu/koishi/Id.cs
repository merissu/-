using RimWorld;
using Verse;

namespace merissu
{
    public class Id : Ability
    {
        public Id() : base() { }

        public Id(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff superEgo = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("SuperEgo"));
            if (superEgo != null)
            {
                pawn.health.RemoveHediff(superEgo);
            }

            HediffDef hediffToAdd = HediffDef.Named("Id");
            if (hediffToAdd != null)
            {
                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);
                Messages.Message(pawn.LabelShort + " 心中的某些东西被解放了！", pawn, MessageTypeDefOf.PositiveEvent);
            }

            return base.Activate(target, dest);
        }
    }
}