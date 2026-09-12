using RimWorld;
using Verse;

namespace merissu
{
    public class DefModExtension_LimitPerMap : DefModExtension
    {
        public int maxCount = 1; 
    }

    public class PlaceWorker_LimitPerMap : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            if (map == null || !(checkingDef is ThingDef thingDef))
                return true;

            DefModExtension_LimitPerMap ext = thingDef.GetModExtension<DefModExtension_LimitPerMap>();
            if (ext == null || ext.maxCount <= 0)
                return true;

            int max = ext.maxCount;
            int count = 0;

            foreach (Thing t in map.listerThings.ThingsOfDef(thingDef))
            {
                if (t == thingToIgnore || t == thing) continue;
                if (++count >= max) return Fail(max);
            }

            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
            {
                if (t == thingToIgnore || t == thing) continue;
                if (t.def.entityDefToBuild == thingDef && ++count >= max) return Fail(max);
            }

            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
            {
                if (t == thingToIgnore || t == thing) continue;
                if (t.def.entityDefToBuild == thingDef && ++count >= max) return Fail(max);
            }

            return true;
        }

        private static AcceptanceReport Fail(int max)
        {
            return "该建筑最多允许存在 {0} 个".Translate(max);
        }
    }
}
