using RimWorld;
using Verse;

namespace merissu
{
    public class sublimemajesty : Ability
    {
        public sublimemajesty() { }

        public sublimemajesty(Pawn pawn) : base(pawn) { }

        public sublimemajesty(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null) return false;

            ThingDef effectDef = ThingDef.Named("SublimeMajestyEffect");
            if (effectDef == null)
            {
                Log.Error("SublimeMajestyEffect ThingDef not found!");
                return false;
            }

            Thing_SublimeMajesty core = ThingMaker.MakeThing(effectDef) as Thing_SublimeMajesty;

            if (core != null)
            {
                core.caster = pawn;
                GenSpawn.Spawn(core, pawn.Position, pawn.Map);
            }
            else
            {
                Log.Error("Failed to create Thing_SublimeMajesty. Check if thingClass in XML is correct.");
            }

            return base.Activate(target, dest);
        }
    }
}