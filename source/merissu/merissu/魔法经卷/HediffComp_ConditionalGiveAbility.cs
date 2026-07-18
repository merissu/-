using RimWorld;
using Verse;

namespace merissu
{

    public class HediffCompProperties_ConditionalGiveAbility : HediffCompProperties
    {
        public AbilityDef abilityDef;
        public HediffDef blockingHediffDef;

        public HediffCompProperties_ConditionalGiveAbility()
        {
            compClass = typeof(HediffComp_ConditionalGiveAbility);
        }
    }

    public class HediffComp_ConditionalGiveAbility : HediffComp
    {
        private HediffCompProperties_ConditionalGiveAbility Props =>
            (HediffCompProperties_ConditionalGiveAbility)props;
        private bool abilityGranted;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            TryGrantAbility();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (parent.pawn.IsHashIntervalTick(60))
                TryGrantAbility();
        }

        public override void CompPostPostRemoved()
        {
            if (abilityGranted)
            {
                parent.pawn.abilities.RemoveAbility(Props.abilityDef);
                abilityGranted = false;
            }
        }

        private void TryGrantAbility()
        {
            bool blocked = parent.pawn.health.hediffSet.HasHediff(Props.blockingHediffDef);

            if (blocked && abilityGranted)
            {
                parent.pawn.abilities.RemoveAbility(Props.abilityDef);
                abilityGranted = false;
            }
            else if (!blocked && !abilityGranted)
            {
                parent.pawn.abilities.GainAbility(Props.abilityDef);
                abilityGranted = true;
            }
        }
    }
}