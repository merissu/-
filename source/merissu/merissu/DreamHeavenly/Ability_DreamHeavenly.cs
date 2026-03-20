using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_DreamHeavenly : Ability
    {
        private static readonly HediffDef FullPowerDef =
            DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_DreamHeavenly() : base() { }

        public Ability_DreamHeavenly(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < 5f)
                {
                    return "灵力不足（需要5层）";
                }
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = this.pawn;

            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < 5f)
                return false;

            fp.Severity -= 5f;

            pawn.health.AddHediff(
                DefDatabase<HediffDef>.GetNamed("DreamHeavenlyInvincible"));

            DreamHeavenlyEmitter emitter =
                (DreamHeavenlyEmitter)ThingMaker.MakeThing(
                    ThingDef.Named("DreamHeavenlyEmitter"));

            emitter.Initialize(pawn, 600);
            GenSpawn.Spawn(emitter, pawn.Position, pawn.Map);

            return true;
        }
    }
}
