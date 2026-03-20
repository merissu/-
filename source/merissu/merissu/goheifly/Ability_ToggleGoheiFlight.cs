using Verse;
using RimWorld;

namespace merissu
{
    public class Ability_ToggleGoheiFlight : Ability
    {
        private static readonly HediffDef FlyingHediffDef =
            HediffDef.Named("Hediff_GoheiFlying");
        public Ability_ToggleGoheiFlight() : base() { }

        public Ability_ToggleGoheiFlight(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = this.pawn;
            if (pawn == null) return false;

            ThingWithComps weapon = pawn.equipment?.Primary;
            if (weapon == null) return false;

            CompGoheiFlight comp = weapon.GetComp<CompGoheiFlight>();
            if (comp == null) return false;

            comp.FlightEnabled = !comp.FlightEnabled;

            Hediff flying = pawn.health.hediffSet
                .GetFirstHediffOfDef(FlyingHediffDef);

            if (comp.FlightEnabled)
            {
                if (flying == null)
                {
                    pawn.health.AddHediff(FlyingHediffDef);
                }
            }
            else
            {
                if (flying != null)
                {
                    pawn.health.RemoveHediff(flying);
                }
            }

            return true;
        }
    }
}
