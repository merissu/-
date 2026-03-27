using RimWorld;
using Verse;
using System.Linq;

namespace merissu
{
    public class HediffComp_NowindHeal : HediffComp
    {
        public HediffCompProperties_NowindHeal Props => (HediffCompProperties_NowindHeal)props;

        private int tickCounter = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            tickCounter++;
            if (tickCounter >= 60)
            {
                tickCounter = 0;
                TryHeal();
            }
        }

        private void TryHeal()
        {
            if (Pawn.health.hediffSet.HasHediff(HediffDef.Named("FullPower")))
            {
                var injuries = Pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(i => i.CanHealNaturally() || i.IsPermanent());

                if (injuries.Any())
                {
                    Hediff_Injury injuryToHeal = injuries.OrderByDescending(i => i.Severity).FirstOrDefault();

                    if (injuryToHeal != null)
                    {
                        injuryToHeal.Heal(1.0f);

                    }
                }
            }
        }
    }

    public class HediffCompProperties_NowindHeal : HediffCompProperties
    {
        public HediffCompProperties_NowindHeal()
        {
            this.compClass = typeof(HediffComp_NowindHeal);
        }
    }
}