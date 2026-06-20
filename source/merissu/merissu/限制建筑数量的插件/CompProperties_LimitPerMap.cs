using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_LimitPerMap : CompProperties
    {
        public int maxCount = 1;

        public CompProperties_LimitPerMap()
        {
            compClass = typeof(CompLimitPerMap);
        }
    }
    public class CompLimitPerMap : ThingComp
    {
        public CompProperties_LimitPerMap Props =>
            (CompProperties_LimitPerMap)props;
    }
    public class PlaceWorker_LimitPerMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            ThingDef thingDef = checkingDef as ThingDef;

            if (thingDef == null)
            {
                return true;
            }
            CompProperties_LimitPerMap limitComp =
                thingDef.GetCompProperties<CompProperties_LimitPerMap>();

            if (limitComp == null)
                return true;

            int count = 0;

            foreach (Thing t in map.listerThings.AllThings)
            {
                if (t.def == thingDef)
                {
                    count++;

                    if (count >= limitComp.maxCount)
                        return $"该建筑最多允许存在 {limitComp.maxCount} 个";
                }
            }

            foreach (Thing t in map.listerThings.AllThings)
            {
                if (t is Blueprint blueprint &&
                    blueprint.def.entityDefToBuild == thingDef)
                {
                    count++;

                    if (count >= limitComp.maxCount)
                        return $"该建筑最多允许存在 {limitComp.maxCount} 个";
                }

                if (t is Frame frame &&
                    frame.def.entityDefToBuild == thingDef)
                {
                    count++;

                    if (count >= limitComp.maxCount)
                        return $"该建筑最多允许存在 {limitComp.maxCount} 个";
                }
            }

            return true;
        }
    }

}