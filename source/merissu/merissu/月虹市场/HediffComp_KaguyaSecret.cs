using System.Collections.Generic;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_KaguyaSecret : HediffComp
    {
        public HediffCompProperties_KaguyaSecret Props => (HediffCompProperties_KaguyaSecret)props;

        private float lastUpSeverity = -1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (parent.pawn.IsHashIntervalTick(60))
            {
                Hediff upHediff = parent.pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.def.defName == "up");

                if (upHediff != null)
                {
                    float currentSeverity = upHediff.Severity;

                    if (lastUpSeverity < 0)
                    {
                        lastUpSeverity = currentSeverity;
                        return;
                    }

                    if (currentSeverity < lastUpSeverity)
                    {
                        TriggerDrop();
                    }

                    lastUpSeverity = currentSeverity;
                }
                else
                {
                    lastUpSeverity = -1f;
                }
            }
        }

        private void TriggerDrop()
        {
            if (Props.dropList.NullOrEmpty()) return;

            ThingDef chosenThing = Props.dropList.RandomElement();
            if (chosenThing != null)
            {
                GenSpawn.Spawn(chosenThing, parent.pawn.PositionHeld, parent.pawn.MapHeld);
                MoteMaker.ThrowText(parent.pawn.DrawPos, parent.pawn.MapHeld, "辉夜姬的秘密宝箱！");
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastUpSeverity, "lastUpSeverity", -1f);
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