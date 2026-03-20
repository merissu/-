using RimWorld;
using Verse;
using System.Linq; 

namespace merissu
{
    public class Ability_MilkyWay : Ability
    {
        public Ability_MilkyWay() : base() { }

        public Ability_MilkyWay(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn.Map == null) return false;

            Hediff cardStatus = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("MarisaCardDeclared"));
            if (cardStatus != null)
            {
                pawn.health.RemoveHediff(cardStatus);
            }

            HediffDef myStatus = HediffDef.Named("MilkyWay");
            if (myStatus != null && !pawn.health.hediffSet.HasHediff(myStatus))
            {
                pawn.health.AddHediff(myStatus);
            }

            Thing thing = ThingMaker.MakeThing(ThingDef.Named("MilkyWayEmitter"));
            GenSpawn.Spawn(thing, pawn.Position, pawn.Map);

            if (thing is Thing_MilkyWayEmitter emitter)
            {
                emitter.Init(pawn);
            }

            return true;
        }
    }
}