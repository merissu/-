using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_FlowerLand : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_FlowerLand() : base() { }

        public Ability_FlowerLand(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                if (fp == null || fp.Severity < 2f)
                {
                    return "灵力不足（需要2层）";
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

            if (fp != null && fp.Severity >= 2f)
            {
                fp.Severity -= 2f;

                return base.Activate(target, dest);
            }

            return false;
        }
    }
}