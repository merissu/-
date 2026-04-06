using Verse;

namespace merissu
{
    public class HediffCompProperties_IceFairy : HediffCompProperties
    {
        public HediffCompProperties_IceFairy()
        {
            this.compClass = typeof(HediffComp_IceFairy);
        }
    }

    public class HediffComp_IceFairy : HediffComp
    {
        private Thing_IceFairy iceCrystal;
        private static readonly ThingDef CrystalDef = ThingDef.Named("IceFairyThing");

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(250))
            {
                CheckCrystal();
            }
        }

        private void CheckCrystal()
        {
            if (Pawn.Spawned && !Pawn.Dead)
            {
                if (iceCrystal == null || !iceCrystal.Spawned || iceCrystal.Destroyed)
                {
                    SpawnIceCrystal();
                }
            }
        }

        private void SpawnIceCrystal()
        {
            iceCrystal = (Thing_IceFairy)ThingMaker.MakeThing(CrystalDef);
            iceCrystal.Init(Pawn);
            GenSpawn.Spawn(iceCrystal, Pawn.Position, Pawn.Map);
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (iceCrystal != null && !iceCrystal.Destroyed)
            {
                iceCrystal.Destroy();
            }
        }
    }
}