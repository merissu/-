using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_AnnnoyingUFO : HediffCompProperties
    {
        public HediffCompProperties_AnnnoyingUFO()
        {
            this.compClass = typeof(HediffComp_AnnnoyingUFO);
        }
    }

    public class HediffComp_AnnnoyingUFO : HediffComp
    {
        private Thing_AnnnoyingUFO ufo;
        private static readonly ThingDef UFODef = ThingDef.Named("AnnnoyingUFO_Thing");

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(200) && Pawn.Spawned && !Pawn.Dead)
            {
                if (ufo == null || !ufo.Spawned || ufo.Destroyed)
                {
                    SpawnUFO();
                }
            }
        }

        private void SpawnUFO()
        {
            ufo = (Thing_AnnnoyingUFO)ThingMaker.MakeThing(UFODef);
            ufo.Init(Pawn);
            GenSpawn.Spawn(ufo, Pawn.Position, Pawn.Map);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (ufo != null && !ufo.Destroyed)
            {
                ufo.Destroy();
            }
        }
    }
}