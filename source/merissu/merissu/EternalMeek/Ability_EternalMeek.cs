using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_EternalMeek : Ability
    {
        public Ability_EternalMeek() : base() { }

        public Ability_EternalMeek(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足";
                }
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn.Map == null) return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null && hp.Severity >= 1f)
            {
                hp.Severity -= 1f;
            }
            else
            {
                return false; 
            }

            Thing_EternalMeekEmitter emitter = (Thing_EternalMeekEmitter)GenSpawn.Spawn(
                ThingDef.Named("EternalMeekEmitter"),
                pawn.Position,
                pawn.Map);

            emitter.Init(pawn, target);

            return base.Activate(target, dest);
        }
    }
}