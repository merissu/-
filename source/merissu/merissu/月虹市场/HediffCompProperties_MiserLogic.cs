using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_MiserLogic : HediffCompProperties
    {
        public HediffDef targetHediffDef; 
        public float refundMultiplier = 0.5f; 
        public float threshold = 0.01f; 

        public HediffCompProperties_MiserLogic()
        {
            compClass = typeof(HediffComp_MiserLogic);
        }
    }

    public class HediffComp_MiserLogic : HediffComp
    {
        public HediffCompProperties_MiserLogic Props => (HediffCompProperties_MiserLogic)props;

        private float lastSeverity = -1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Hediff target = parent.pawn.health.hediffSet.GetFirstHediffOfDef(Props.targetHediffDef);

            if (target == null) return;

            if (lastSeverity < 0)
            {
                lastSeverity = target.Severity;
                return;
            }

            float diff = lastSeverity - target.Severity;

            if (diff > Props.threshold && !Mathf.Approximately(diff, 0.5f))
            {
                float refund = diff * Props.refundMultiplier;
                target.Severity += refund;
            }

            lastSeverity = target.Severity;
        }
    }
}