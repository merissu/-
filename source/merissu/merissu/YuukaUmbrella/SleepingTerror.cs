using RimWorld;
using Verse;

namespace merissu
{
    public class SleepingTerror : Ability
    {
        public SleepingTerror() : base() { }

        public SleepingTerror(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            HediffDef hediffToAdd = HediffDef.Named("SleepingTerror");

            if (hediffToAdd != null)
            {
                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);

                Messages.Message(pawn.LabelShort + " 沉睡的恐怖~Sleeping Terror", pawn, MessageTypeDefOf.PositiveEvent);
            }

            return base.Activate(target, dest);
        }
    }
}