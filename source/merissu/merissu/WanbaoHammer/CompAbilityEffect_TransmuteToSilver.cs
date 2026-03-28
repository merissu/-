using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_AbilityTransmuteToSilver : CompProperties_AbilityEffect
    {
        public int fallbackSilver = 9; 

        public CompProperties_AbilityTransmuteToSilver()
        {
            compClass = typeof(CompAbilityEffect_TransmuteToSilver);
        }
    }

    public class CompAbilityEffect_TransmuteToSilver : CompAbilityEffect
    {
        public new CompProperties_AbilityTransmuteToSilver Props =>
            (CompProperties_AbilityTransmuteToSilver)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Thing t = target.Thing;
            return t != null && t.MapHeld != null;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!CanApplyOn(target, LocalTargetInfo.Invalid))
            {
                if (throwMessages)
                    Messages.Message("目标无效。", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Thing t = target.Thing;
            if (t == null || t.MapHeld == null) return;

            Map map = t.MapHeld;
            IntVec3 pos = t.PositionHeld;

            float perUnitValue = 0f;
            try
            {
                perUnitValue = t.MarketValue; 
            }
            catch
            {
                perUnitValue = 0f;
            }

            int stackCount = Mathf.Max(1, t.stackCount);
            float totalValue = perUnitValue * stackCount;

            int silverToSpawn = totalValue > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(totalValue))
                : Mathf.Max(1, Props.fallbackSilver);

            t.Destroy(DestroyMode.Vanish);

            while (silverToSpawn > 0)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                int oneStack = Mathf.Min(silverToSpawn, ThingDefOf.Silver.stackLimit);
                silver.stackCount = oneStack;
                silverToSpawn -= oneStack;

                GenPlace.TryPlaceThing(silver, pos, map, ThingPlaceMode.Near);
            }
        }
    }
}