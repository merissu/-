using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_HiziriPower : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_HiziriPower() : base() {}
        public Ability_HiziriPower(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                if (fp == null || fp.Severity < 5f)
                {
                    return "灵力不足（需要5层）";
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

            if (fp != null && fp.Severity >= 5f)
            {
                fp.Severity -= 5f;

                return base.Activate(target, dest);
            }

            return false;
        }
    }
}