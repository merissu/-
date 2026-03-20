using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_NoachianDeluge : Ability
    {
        public Ability_NoachianDeluge() : base() { }

        public Ability_NoachianDeluge(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
                if (hp == null || hp.Severity < 1f) return "灵力不足";

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null) return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f) return false;

            hp.Severity -= 1f;

            Thing_NoachianDelugeEmitter emitter = (Thing_NoachianDelugeEmitter)GenSpawn.Spawn(
                ThingDef.Named("NoachianDelugeEmitter"),
                pawn.Position,
                pawn.Map);

            emitter.Init(pawn, target);

            return base.Activate(target, dest);
        }
    }
}