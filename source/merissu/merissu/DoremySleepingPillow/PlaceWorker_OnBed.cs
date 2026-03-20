using RimWorld;
using Verse;

namespace merissu
{
    public class PlaceWorker_OnBed : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            foreach (Thing t in map.thingGrid.ThingsListAt(loc))
            {
                if (t.def.IsBed) return true;
            }

            return "MustPlaceOnBed".CanTranslate() ? "MustPlaceOnBed".Translate().ToString() : "必须放置在床上。";
        }
        public override bool ForceAllowPlaceOver(BuildableDef otherDef)
        {
            var thingDef = otherDef as ThingDef;
            return thingDef != null && thingDef.IsBed;
        }
    }
}