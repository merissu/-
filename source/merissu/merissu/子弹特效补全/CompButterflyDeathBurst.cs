using Verse;

namespace merissu
{
    public class CompProperties_ButterflyDeathBurst : CompProperties
    {
        public ThingDef petalMoteDef;
        public int minPetals = 3;
        public int maxPetals = 4;

        public CompProperties_ButterflyDeathBurst()
        {
            compClass = typeof(CompButterflyDeathBurst);
        }
    }

    public class CompButterflyDeathBurst : ThingComp
    {
        public CompProperties_ButterflyDeathBurst Props => (CompProperties_ButterflyDeathBurst)props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap == null || Props.petalMoteDef == null)
                return;

            int count = Rand.RangeInclusive(Props.minPetals, Props.maxPetals);
            for (int i = 0; i < count; i++)
            {
                Mote_ButterflyPetal petal = (Mote_ButterflyPetal)ThingMaker.MakeThing(Props.petalMoteDef);
                GenSpawn.Spawn(petal, this.parent.Position, previousMap);
            }
        }
    }
}