using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_NightFogPhantomKiller : Ability
    {
        public Ability_NightFogPhantomKiller() : base() { }

        public Ability_NightFogPhantomKiller(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

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
            if (pawn == null || !pawn.Spawned) return false;
            if (!target.HasThing || !(target.Thing is Pawn)) return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null && hp.Severity >= 1f)
            {
                hp.Severity -= 1f; 
            }
            else
            {
                return false;
            }

            Pawn targetPawn = (Pawn)target.Thing;

            Thing controllerThing = GenSpawn.Spawn(
                ThingDef.Named("NightFogKnifeController"),
                pawn.Position,
                pawn.Map
            );

            Thing_NightFogKnifeController controller =
                controllerThing as Thing_NightFogKnifeController;

            if (controller != null)
            {
                controller.caster = pawn;
                controller.target = targetPawn;
            }

            return base.Activate(target, dest);
        }
    }
}