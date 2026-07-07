using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class Ability_FourOfAKind : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");

        public Ability_FourOfAKind() : base() { }
        public Ability_FourOfAKind(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < 1f)
                {
                    return "符卡不足（需要1张）";
                }
                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < 1f)
            {
                return false;
            }

            float originalSeverity = fp.Severity;
            fp.Severity = 0f;

            bool result = base.Activate(target, dest);

            float returnSeverity = Mathf.Max(0f, originalSeverity - 1f);
            fp.Severity = returnSeverity;

            return result;
        }
    }
}