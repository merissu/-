using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_SpawnSilver : CompProperties
    {
        public IntRange amountRange = new IntRange(50, 200);
        public ThingDef thingToSpawn;

        public CompProperties_SpawnSilver()
        {
            this.compClass = typeof(CompSpawnSilverOnUse);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (this.thingToSpawn == null)
            {
                this.thingToSpawn = ThingDefOf.Silver;
            }
        }
    }

    public class CompSpawnSilverOnUse : ThingComp
    {
        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);

            CompProperties_SpawnSilver props = (CompProperties_SpawnSilver)this.props;

            int count = props.amountRange.RandomInRange;
            if (count > 0 && props.thingToSpawn != null)
            {
                Thing silver = ThingMaker.MakeThing(props.thingToSpawn);
                silver.stackCount = count;
                GenPlace.TryPlaceThing(silver, ingester.Position, ingester.Map, ThingPlaceMode.Near);
            }
        }
    }
}