using System.Collections.Generic;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_PengLaiMedicineHeal : HediffComp
    {
        public int tick;

        public HediffCompProperties_PengLaiMedicineHeal Props => (HediffCompProperties_PengLaiMedicineHeal)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (base.Pawn.Dead)
            {
                return;
            }

            int badHediffCount = 0;
            List<Hediff> hediffs = base.Pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].def.isBad)
                {
                    badHediffCount++;
                }
            }

            int interval = 2500 - (badHediffCount * 250);

            if (interval <= 0)
            {
                interval = 1;
            }

            if (this.tick % interval == 0)
            {
                HealthUtility.FixWorstHealthCondition(base.Pawn);
            }

            if (this.tick >= int.MaxValue)
            {
                this.tick = 0;
            }
            this.tick++;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref this.tick, "tick", 0);
        }
    }
}