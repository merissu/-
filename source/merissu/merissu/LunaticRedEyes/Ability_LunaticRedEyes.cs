using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_LunaticRedEyes : Ability
    {
        private const float EnergyCost = 5f;

        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_LunaticRedEyes() : base() { }

        public Ability_LunaticRedEyes(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                if (fp == null || fp.Severity < EnergyCost)
                {
                    return "灵力不足（需要五层）";
                }
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null) return false;

            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < EnergyCost)
            {
                Messages.Message("灵力不足", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            fp.Severity -= EnergyCost;

            HediffDef myStatus = HediffDef.Named("LunaticRedEyes");
            if (myStatus != null && !pawn.health.hediffSet.HasHediff(myStatus))
            {
                pawn.health.AddHediff(myStatus);
            }

            Thing_LunaticRedEyesEmitter emitter =
                (Thing_LunaticRedEyesEmitter)ThingMaker.MakeThing(
                    ThingDef.Named("LunaticRedEyesEmitter"));

            emitter.Init(pawn);
            GenSpawn.Spawn(emitter, pawn.Position, pawn.Map);

            return true;
        }
    }
}