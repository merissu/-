using System.Linq;
using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_ShinkiRegen : HediffCompProperties
    {
        public HediffCompProperties_ShinkiRegen()
        {
            compClass = typeof(HediffComp_ShinkiRegen);
        }
    }

    public class HediffComp_ShinkiRegen : HediffComp
    {
        private int tickCounter = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn?.health == null) return;

            tickCounter++;
            if (tickCounter >= 60)
            {
                tickCounter = 0;
                TryHeal();
            }
        }

        private void TryHeal()
        {
            var injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.CanHealNaturally() || i.IsPermanent())
                .OrderByDescending(i => i.Severity)
                .ToList();

            if (injuries.Any())
            {
                injuries.First().Heal(0.5f);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }
}