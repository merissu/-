using Verse;
using UnityEngine;

namespace merissu
{
    public class CompProperties_DestroySpawnMote : CompProperties
    {
        public string moteDefName = "Mote_BulletDestroy";

        public CompProperties_DestroySpawnMote()
        {
            compClass = typeof(CompDestroySpawnMote);
        }
    }

    public class CompDestroySpawnMote : ThingComp
    {
        private bool spawned;

        public CompProperties_DestroySpawnMote Props =>
            (CompProperties_DestroySpawnMote)props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (spawned)
                return;

            spawned = true;

            if (previousMap == null)
                return;

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.moteDefName);

            if (moteDef == null)
                return;

            Mote_BulletDestroyFade mote =
                (Mote_BulletDestroyFade)ThingMaker.MakeThing(moteDef);

            mote.exactPosition = parent.DrawPos;

            GenSpawn.Spawn(
                mote,
                parent.PositionHeld,
                previousMap);
        }
    }
}