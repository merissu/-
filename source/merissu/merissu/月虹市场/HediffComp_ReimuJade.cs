using Verse;

namespace merissu
{
    public class HediffCompProperties_ReimuJade : HediffCompProperties
    {
        public HediffCompProperties_ReimuJade()
        {
            this.compClass = typeof(HediffComp_ReimuJade);
        }
    }

    public class HediffComp_ReimuJade : HediffComp
    {
        private Thing_ReimuJade jade;

        private static readonly ThingDef JadeDef = ThingDef.Named("ReimuJadeThing");

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(60))
            {
                if (jade == null || !jade.Spawned || jade.Destroyed)
                {
                    SpawnJade();
                }
            }
        }

        private void SpawnJade()
        {
            if (Pawn.Map != null && !Pawn.Dead)
            {
                jade = (Thing_ReimuJade)ThingMaker.MakeThing(JadeDef);
                jade.Init(Pawn);
                GenSpawn.Spawn(jade, Pawn.Position, Pawn.Map);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (jade != null && !jade.Destroyed)
            {
                jade.Destroy();
            }
        }
    }
}