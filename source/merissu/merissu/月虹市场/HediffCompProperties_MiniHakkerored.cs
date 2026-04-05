using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_MiniHakkerored : HediffCompProperties
    {
        public ThingDef orbDef; 
        public float radius = 3f;

        public HediffCompProperties_MiniHakkerored()
        {
            this.compClass = typeof(HediffComp_MiniHakkerored);
        }
    }

    public class HediffComp_MiniHakkerored : HediffComp
    {
        private Thing_StarlightOrbRed_Follower spawnedOrb;

        public HediffCompProperties_MiniHakkerored Props => (HediffCompProperties_MiniHakkerored)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            SpawnOrb();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (spawnedOrb == null || spawnedOrb.Destroyed)
            {
                SpawnOrb();
            }
        }

        private void SpawnOrb()
        {
            if (Pawn.Map != null && Props.orbDef != null)
            {
                var newThing = ThingMaker.MakeThing(Props.orbDef);
                spawnedOrb = newThing as Thing_StarlightOrbRed_Follower;

                if (spawnedOrb != null)
                {
                    spawnedOrb.Init(Pawn);
                    GenSpawn.Spawn(spawnedOrb, Pawn.Position, Pawn.Map);
                }
                else
                {
                    Log.Error($"[merissu] {Props.orbDef.defName} 的 thingClass 不是 Thing_StarlightOrbRed_Follower!");
                }
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