using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_RoyalFlare : Ability
    {
        public Ability_RoyalFlare() : base() { }

        public Ability_RoyalFlare(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 2f)
                {
                    return "灵力不足 (需要2层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 2f)
            {
                return false;
            }

            hp.Severity -= 2f;

            var ctrl = (Thing_RoyalFlareController)ThingMaker.MakeThing(
                ThingDef.Named("RoyalFlareController"));

            ctrl.Init(pawn);
            GenSpawn.Spawn(ctrl, pawn.Position, pawn.Map);

            return true;
        }
    }
}