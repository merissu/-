using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class MerissuDefOf
    {
        public static AbilityDef Merissu_WanbaoHammerRepair;
        public static ThingDef WanbaoHammer;
        public static ThingDef Merissu_WarFlyer;
        public static JobDef Merissu_GreatswordSweep;

        static MerissuDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MerissuDefOf));
        }
    }
}