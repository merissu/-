using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Projectile_StElmoFireball : Projectile
    {
        protected override void Tick()
        {
            base.Tick();

            if (Map == null) return;

            if (Find.TickManager.TicksGame % 5 != 0) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(
                ThingDef.Named("Mote_StElmoFireTrail"));

            mote.exactPosition = ExactPosition;
            mote.Scale = Rand.Range(0.4f, 0.7f);

            mote.SetVelocity(
                Rand.Range(0f, 360f),
                Rand.Range(0.1f, 0.3f));

            GenSpawn.Spawn(mote, ExactPosition.ToIntVec3(), Map);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 pos = Position;

            base.Impact(hitThing, blockedByShield);

            GenExplosion.DoExplosion(
                pos,
                map,
                3.5f,
                DamageDefOf.Flame,
                launcher,
                damAmount: 1000);

            GenSpawn.Spawn(
                ThingDef.Named("StElmoFirePillar"),
                pos,
                map);
        }
    }
}