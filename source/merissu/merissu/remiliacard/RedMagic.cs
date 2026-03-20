using RimWorld;
using Verse;

namespace merissu
{
    public class RedMagic : Ability
    {
        private static readonly HediffDef FullPowerDef =
            HediffDef.Named("FullPower");
        public RedMagic() : base() { }

        public RedMagic(Pawn pawn) : base(pawn) { }

        public RedMagic(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet
                    .GetFirstHediffOfDef(FullPowerDef);

                if (hp == null || hp.Severity < 5f)
                {
                    return "灵力不足 (需要5层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet
                .GetFirstHediffOfDef(FullPowerDef);

            if (hp == null || hp.Severity < 5f)
                return false;

            hp.Severity -= 5f;

            GameConditionDef def =
                DefDatabase<GameConditionDef>.GetNamed("RedMagicFog");

            GameCondition condition =
                GameConditionMaker.MakeCondition(def, 60000);

            pawn.Map.gameConditionManager.RegisterCondition(condition);

            Messages.Message(
                "红雾笼罩了整张地图……",
                pawn,
                MessageTypeDefOf.ThreatBig
            );

            return base.Activate(target, dest);
        }
    }
}