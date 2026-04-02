using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class HediffCompProperties_SpiritualPowerHeal : HediffCompProperties
    {
        public HediffCompProperties_SpiritualPowerHeal()
        {
            compClass = typeof(HediffComp_SpiritualPowerHeal);
        }
    }

    public class HediffComp_SpiritualPowerHeal : HediffComp
    {
        public HediffCompProperties_SpiritualPowerHeal Props => (HediffCompProperties_SpiritualPowerHeal)props;

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (Pawn.Downed)
            {
                TriggerHealOnDowned();
            }
        }

        private void TriggerHealOnDowned()
        {
            float currentSeverity = parent.Severity;

            if (currentSeverity > 0.5f)
            {
                float totalHealAmount = currentSeverity * 20f;
                ApplyHealing(totalHealAmount);

                HediffDef knockbackDef = HediffDef.Named("MagicKnockback");
                if (knockbackDef != null)
                {
                    Pawn.health.AddHediff(knockbackDef);
                }

                parent.Severity = 0.01f;

                MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "满目疮痍 " + totalHealAmount.ToString("F1"));

                SoundDef.Named("MagicKnockback").PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
            }
        }
        private void ApplyHealing(float amount)
        {
            IEnumerable<Hediff_Injury> injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.CanHealNaturally());

            float remainingHeal = amount;

            foreach (Hediff_Injury injury in injuries)
            {
                if (remainingHeal <= 0) break;

                float healStep = UnityEngine.Mathf.Min(injury.Severity, remainingHeal);
                injury.Heal(healStep);
                remainingHeal -= healStep;
            }
        }
    }
}