using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_AbilityUpgradeQuality : CompProperties_AbilityEffect
    {
        public HediffDef requiredHediff;   
        public float severityCost = 1f;    

        public CompProperties_AbilityUpgradeQuality()
        {
            compClass = typeof(CompAbilityEffect_UpgradeQuality);
        }
    }

    public class CompAbilityEffect_UpgradeQuality : CompAbilityEffect
    {
        public new CompProperties_AbilityUpgradeQuality Props => (CompProperties_AbilityUpgradeQuality)props;

        private Hediff FullPowerHediff
        {
            get
            {
                if (parent?.pawn?.health?.hediffSet == null || Props.requiredHediff == null) return null;
                return parent.pawn.health.hediffSet.GetFirstHediffOfDef(Props.requiredHediff);
            }
        }

        private bool HasEnoughFullPower()
        {
            Hediff h = FullPowerHediff;
            return h != null && h.Severity >= Props.severityCost;
        }

        private static bool TryGetUpgradableQuality(Thing t, out CompQuality comp, out QualityCategory quality)
        {
            comp = t?.TryGetComp<CompQuality>();
            if (comp == null)
            {
                quality = default;
                return false;
            }

            quality = comp.Quality;
            return quality < QualityCategory.Legendary;
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (!HasEnoughFullPower())
            {
                reason = "需要一层灵力";
                return true;
            }

            reason = null;
            return false;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!HasEnoughFullPower()) return false;

            Thing t = target.Thing;
            if (t == null) return false;

            return TryGetUpgradableQuality(t, out _, out _);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!HasEnoughFullPower())
            {
                if (throwMessages) Messages.Message("灵力不足，无法发动。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            Thing t = target.Thing;
            if (t == null)
            {
                if (throwMessages) Messages.Message("目标无效。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!TryGetUpgradableQuality(t, out _, out _))
            {
                if (throwMessages) Messages.Message("目标没有品质，或已是传说品质。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing t = target.Thing;
            if (!TryGetUpgradableQuality(t, out CompQuality comp, out QualityCategory oldQ)) return;

            QualityCategory newQ = (QualityCategory)((int)oldQ + 1);
            comp.SetQuality(newQ, ArtGenerationContext.Colony);

            Hediff h = FullPowerHediff;
            if (h != null)
            {
                h.Severity = Mathf.Max(0f, h.Severity - Props.severityCost);
            }

            Messages.Message($"品质提升：{t.LabelCap}  {oldQ} → {newQ}", t, MessageTypeDefOf.PositiveEvent);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return HasEnoughFullPower() && base.AICanTargetNow(target);
        }
    }
}