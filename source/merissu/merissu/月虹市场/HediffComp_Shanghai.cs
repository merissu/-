using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_Shanghai : HediffCompProperties
    {
        public HediffCompProperties_Shanghai()
        {
            this.compClass = typeof(HediffComp_Shanghai);
        }
    }
    public class HediffComp_Shanghai : HediffComp
    {
        private Thing_ShanghaiDoll spawnedDoll;

        private static readonly ThingDef DollDef = ThingDef.Named("ShanghaiDoll_Thing");

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(60))
            {
                if (spawnedDoll == null || !spawnedDoll.Spawned || spawnedDoll.Destroyed)
                {
                    SpawnDoll();
                }
            }
        }

        private void SpawnDoll()
        {
            if (Pawn.Map != null && !Pawn.Dead)
            {
                spawnedDoll = (Thing_ShanghaiDoll)GenSpawn.Spawn(DollDef, Pawn.Position, Pawn.Map);
                spawnedDoll.Init(Pawn);
            }
        }
        public override void CompPostPostRemoved()
        {
            spawnedDoll?.Destroy();
            base.CompPostPostRemoved();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref spawnedDoll, "spawnedDoll");
        }
    }
}