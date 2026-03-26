using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace merissu
{
    public class HediffComp_MagicSnow : HediffComp
    {
        private const int CheckInterval = 2500;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(CheckInterval))
            {
                ApplyMagicEffect();
            }
        }

        private void ApplyMagicEffect()
        {
            Hediff fullPowerHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (fullPowerHediff != null)
            {
                fullPowerHediff.Severity -= 1f;

                List<Hediff> toRemove = Pawn.health.hediffSet.hediffs
                    .Where(h => h.def.isBad && h.Visible)
                    .ToList();

                foreach (Hediff h in toRemove)
                {
                    Pawn.health.RemoveHediff(h);
                }

            }
        }
    }

    public class HediffCompProperties_MagicSnow : HediffCompProperties
    {
        public HediffCompProperties_MagicSnow()
        {
            compClass = typeof(HediffComp_MagicSnow);
        }
    }
}