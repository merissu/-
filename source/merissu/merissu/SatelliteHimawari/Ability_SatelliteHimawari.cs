using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_SatelliteHimawari : Ability
    {
        public Ability_SatelliteHimawari() : base() { }

        public Ability_SatelliteHimawari(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }
        public override AcceptanceReport CanCast
        {
            get
            {
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

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f)
            {
                return false;
            }

            hp.Severity -= 1f;

            SatelliteHimawariEmitter emitter =
                (SatelliteHimawariEmitter)GenSpawn.Spawn(
                    ThingDef.Named("SatelliteHimawariEmitter"),
                    pawn.Position,
                    pawn.Map
                );

            emitter.caster = pawn;

            return base.Activate(target, dest);
        }
    }
}