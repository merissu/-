using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_MiniHakkero : HediffCompProperties
    {
        public ThingDef orbDef;
        public float radius = 3f; 
        public HediffCompProperties_MiniHakkero()
        {
            compClass = typeof(HediffComp_MiniHakkero);
        }
    }

    public class HediffComp_MiniHakkero : HediffComp
    {
        private Thing_StarlightOrb_Follower spawnedOrb;
        private static readonly Material BlueMat = MaterialPool.MatFrom("Other/StarlightLaser_Blue", ShaderDatabase.MoteGlow);

        public HediffCompProperties_MiniHakkero Props => (HediffCompProperties_MiniHakkero)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(60))
            {
                if (spawnedOrb == null || spawnedOrb.Destroyed)
                {
                    SpawnOrb();
                }
            }
        }

        private void SpawnOrb()
        {
            if (Pawn.Spawned && !Pawn.Dead)
            {
                spawnedOrb = (Thing_StarlightOrb_Follower)ThingMaker.MakeThing(Props.orbDef);
                spawnedOrb.Init(Pawn, BlueMat);
                GenSpawn.Spawn(spawnedOrb, Pawn.Position, Pawn.Map);
            }
        }
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (spawnedOrb != null && !spawnedOrb.Destroyed)
            {
                spawnedOrb.Destroy();
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref spawnedOrb, "spawnedOrb");
        }
    }
}