using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_AbilityRepairFull : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityRepairFull()
        {
            compClass = typeof(CompAbilityEffect_RepairFull);
        }
    }

    public class CompAbilityEffect_RepairFull : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing thing = target.Thing;
            if (thing == null) return;
            if (!thing.def.useHitPoints) return;

            thing.HitPoints = thing.MaxHitPoints;

            Messages.Message(
                $"已修复：{thing.LabelCap}",
                thing,
                MessageTypeDefOf.PositiveEvent
            );
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Thing thing = target.Thing;
            if (thing == null) return false;
            if (!thing.def.useHitPoints) return false;
            return base.Valid(target, throwMessages);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Thing thing = target.Thing;
            return thing != null && thing.def.useHitPoints && thing.HitPoints < thing.MaxHitPoints;
        }
    }
}