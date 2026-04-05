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

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            SpawnDoll();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (spawnedDoll == null || !spawnedDoll.Spawned || spawnedDoll.Destroyed)
            {
                SpawnDoll();
            }
        }

        private void SpawnDoll()
        {
            if (Pawn.Map != null)
            {
                ThingDef dollDef = ThingDef.Named("ShanghaiDoll_Thing");
                spawnedDoll = (Thing_ShanghaiDoll)GenSpawn.Spawn(dollDef, Pawn.Position, Pawn.Map);
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