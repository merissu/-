using Verse;
using UnityEngine;

namespace merissu
{
    public class Hediff_ThreeHeavenlyDrops : HediffWithComps
    {
        private int ticksUntilRemoval = 2500;

        public override void Tick()
        {
            base.Tick();

            if (this.Severity >= 3f)
            {
                ticksUntilRemoval--;

                if (ticksUntilRemoval <= 0)
                {
                    this.pawn.health.RemoveHediff(this);
                }
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                if (this.Severity >= 3f)
                {
                    return (ticksUntilRemoval / 60).ToString() + "s";
                }
                return base.LabelInBrackets;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilRemoval, "ticksUntilRemoval", 2500);
        }
    }
}