using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_InfraredMoon : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_InfraredMoon() : base() { }
        public Ability_InfraredMoon(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                if (fp == null || fp.Severity < 1f)
                {
                    return "灵力不足（需要1层）";
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

            if (fp != null && fp.Severity >= 1f)
            {
                fp.Severity -= 1f;

                return base.Activate(target, dest);
            }

            return false;
        }
    }
}