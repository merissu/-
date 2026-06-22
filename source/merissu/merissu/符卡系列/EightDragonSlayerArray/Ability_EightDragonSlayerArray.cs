using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_EightDragonSlayerArray : Ability
    {
        public Ability_EightDragonSlayerArray() : base() { }

        public Ability_EightDragonSlayerArray(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            HediffDef cardDef = HediffDef.Named("ReimuCardDeclared");
            if (cardDef != null)
            {
                Hediff cardHediff = pawn.health.hediffSet.GetFirstHediffOfDef(cardDef);
                if (cardHediff != null)
                    pawn.health.RemoveHediff(cardHediff);
            }

            HediffDef casterLockDef = HediffDef.Named("EightDragonCasterLock");
            if (!pawn.health.hediffSet.HasHediff(casterLockDef))
            {
                pawn.health.AddHediff(casterLockDef);
            }

            ThingDef animDef = ThingDef.Named("EightDragonSlayerArrayAnimation");
            var anim = (Thing_EightDragonSlayerArrayAnimation)ThingMaker.MakeThing(animDef);
            anim.Init(pawn);
            GenSpawn.Spawn(anim, pawn.Position, pawn.Map);

            return true;
        }
    }
}