using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_KaguyaSecret : HediffComp
    {
        public HediffCompProperties_KaguyaSecret Props => (HediffCompProperties_KaguyaSecret)props;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit)
        {
            base.Notify_PawnDied(dinfo, culprit);

            if (Props.dropList.NullOrEmpty()) return;

            ThingDef chosenThing = Props.dropList.RandomElement();

            if (chosenThing != null)
            {
                GenSpawn.Spawn(chosenThing, parent.pawn.PositionHeld, parent.pawn.MapHeld);

                MoteMaker.ThrowText(parent.pawn.DrawPos, parent.pawn.MapHeld, "辉夜姬的秘密宝箱!");
            }
        }
    }

    public class HediffCompProperties_KaguyaSecret : HediffCompProperties
    {
        public List<ThingDef> dropList; 

        public HediffCompProperties_KaguyaSecret()
        {
            this.compClass = typeof(HediffComp_KaguyaSecret);
        }
    }
}