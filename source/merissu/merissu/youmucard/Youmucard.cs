using RimWorld;
using Verse;

namespace merissu
{
    public class Youmucard : Ability
    {
        public Youmucard() : base() { }

        public Youmucard(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            HediffDef hediffToAdd = HediffDef.Named("Youmucard");

            if (hediffToAdd != null)
            {
                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);

                Messages.Message(pawn.LabelShort + " 开始了符卡宣言！", pawn, MessageTypeDefOf.PositiveEvent);
            }

            return base.Activate(target, dest);
        }
    }
}
