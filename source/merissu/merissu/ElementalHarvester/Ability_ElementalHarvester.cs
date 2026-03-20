using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_ElementalHarvester : Ability
    {
        public Ability_ElementalHarvester() : base() { }

        public Ability_ElementalHarvester(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet
                    .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                    return "灵力不足";

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 1f)
                return false;

            hp.Severity -= 1f;

            Thing_ElementalHarvester field =
                (Thing_ElementalHarvester)ThingMaker.MakeThing(
                    ThingDef.Named("ElementalHarvesterField"));

            GenSpawn.Spawn(field, pawn.Position, pawn.Map);
            field.Init(pawn);

            return true;
        }
    }
}
