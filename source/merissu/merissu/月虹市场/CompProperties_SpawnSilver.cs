using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_SpawnSilver : CompProperties
    {
        public IntRange amountRange = new IntRange(50, 200); 
        public ThingDef thingToSpawn = ThingDefOf.Silver;

        public CompProperties_SpawnSilver()
        {
            this.compClass = typeof(CompSpawnSilverOnUse);
        }
    }

    public class CompSpawnSilverOnUse : ThingComp
    {
        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);

            CompProperties_SpawnSilver props = (CompProperties_SpawnSilver)this.props;

            int count = props.amountRange.RandomInRange;
            if (count > 0)
            {
                Thing silver = ThingMaker.MakeThing(props.thingToSpawn);
                silver.stackCount = count;
                GenPlace.TryPlaceThing(silver, ingester.Position, ingester.Map, ThingPlaceMode.Near);

            }
        }
    }
}