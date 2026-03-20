using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_EightDragonSlayerArray : Ability
    {
        public Ability_EightDragonSlayerArray() : base() { }

        public Ability_EightDragonSlayerArray(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            HediffDef cardDef = HediffDef.Named("ReimuCardDeclared");
            if (cardDef != null)
            {
                Hediff cardHediff = pawn.health.hediffSet.GetFirstHediffOfDef(cardDef);
                if (cardHediff != null)
                {
                    pawn.health.RemoveHediff(cardHediff);
                }
            }

            if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("EightDragonCasterLock")))
            {
                pawn.health.AddHediff(HediffDef.Named("EightDragonCasterLock"));
            }

            Thing thing = ThingMaker.MakeThing(ThingDef.Named("EightDragonSlayerArray"));

            GenSpawn.Spawn(thing, pawn.Position, pawn.Map);

            if (thing is merissu.Thing_FlowingArray flowingArray)
            {
                flowingArray.Init(pawn);
            }

            return true;
        }
    }
}