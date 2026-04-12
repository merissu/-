using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_WheatRice : Ability
    {
        public Ability_WheatRice() : base() { }
        public Ability_WheatRice(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            HediffDef hpDef = HediffDef.Named("spiritualpower");

            if (pawn != null && hpDef != null)
            {
                Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hpDef);

                if (existingHediff != null)
                {
                    existingHediff.Severity += 1f;
                }
                else
                {
                    Hediff newHediff = HediffMaker.MakeHediff(hpDef, pawn);
                    newHediff.Severity = 1f;
                    pawn.health.AddHediff(newHediff);
                }
            }

            return base.Activate(target, dest);
        }
    }
}