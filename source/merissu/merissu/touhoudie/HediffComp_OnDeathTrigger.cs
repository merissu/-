using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class HediffComp_OnDeathTrigger : HediffComp
    {
        private bool triggered;

        public HediffCompProperties_OnDeathTrigger Props =>
            (HediffCompProperties_OnDeathTrigger)props;

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

            // 播放音效
            Props.soundDef?.PlayOneShot(
                new TargetInfo(pos, map)
            );

            // 掉落物品
            if (Props.thingDefs != null)
            {
                foreach (ThingDef def in Props.thingDefs)
                {
                    Thing thing = ThingMaker.MakeThing(def);
                    thing.stackCount = Props.stackCount;

                    GenPlace.TryPlaceThing(
                        thing,
                        pos,
                        map,
                        ThingPlaceMode.Near
                    );
                }
            }
        }
    }
}
