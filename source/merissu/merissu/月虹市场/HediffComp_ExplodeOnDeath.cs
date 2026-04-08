using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_ExplodeOnDeath : HediffComp
    {
        private bool triggered = false; 

        public HediffCompProperties_ExplodeOnDeath Props => (HediffCompProperties_ExplodeOnDeath)props;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit)
        {
            base.Notify_PawnDied(dinfo, culprit);

            if (triggered) return;
            triggered = true;

            Pawn pawn = parent.pawn;
            if (pawn == null) return;

            Map map = pawn.MapHeld;
            IntVec3 pos = pawn.PositionHeld;

            if (map == null) return;

            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: 3.5f,                       
                damType: DamageDefOf.Bomb,
                instigator: null,
                damAmount: 40,                      
                armorPenetration: -1f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0.1f,           
                damageFalloff: false
            );
        }
    }

    public class HediffCompProperties_ExplodeOnDeath : HediffCompProperties
    {
        public HediffCompProperties_ExplodeOnDeath()
        {
            compClass = typeof(HediffComp_ExplodeOnDeath);
        }
    }
}