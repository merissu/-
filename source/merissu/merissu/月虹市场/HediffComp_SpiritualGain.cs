using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class HediffComp_SpiritualGain : HediffComp
    {
        private Dictionary<string, float> lastSeverities = new Dictionary<string, float>();

        private static readonly List<string> TargetHediffs = new List<string> { "FullPower", "up" };
        private const string ResultHediffDefName = "spiritualpower";

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref lastSeverities, "lastSeverities", LookMode.Value, LookMode.Value);

            if (lastSeverities == null)
            {
                lastSeverities = new Dictionary<string, float>();
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(20))
            {
                CheckSeverityDrop();
            }
        }

        private void CheckSeverityDrop()
        {
            foreach (var defName in TargetHediffs)
            {
                HediffDef targetDef = HediffDef.Named(defName);
                if (targetDef == null) continue;

                Hediff target = Pawn.health.hediffSet.GetFirstHediffOfDef(targetDef);
                float currentSev = target?.Severity ?? 0f;

                if (lastSeverities.TryGetValue(defName, out float lastSev))
                {
                    if (currentSev < lastSev)
                    {
                        TriggerSpiritualPower();
                    }
                }

                lastSeverities[defName] = currentSev;
            }
        }

        private void TriggerSpiritualPower()
        {
            HediffDef powerDef = HediffDef.Named(ResultHediffDefName);
            if (powerDef == null) return;

            Hediff existing = Pawn.health.hediffSet.GetFirstHediffOfDef(powerDef);

            if (existing != null)
            {
                existing.Severity += 0.5f;
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(powerDef, Pawn);
                newHediff.Severity = 0.5f;
                Pawn.health.AddHediff(newHediff);
            }
        }
    }

    public class HediffCompProperties_SpiritualGain : HediffCompProperties
    {
        public HediffCompProperties_SpiritualGain()
        {
            this.compClass = typeof(HediffComp_SpiritualGain);
        }
    }
}