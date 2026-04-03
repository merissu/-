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

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            SpawnJade();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (jade == null || !jade.Spawned)
            {
                SpawnJade();
            }
        }

        private void SpawnJade()
        {
            if (Pawn.Map != null)
            {
                jade = (Thing_ReimuJade)ThingMaker.MakeThing(ThingDef.Named("ReimuJadeThing"));
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