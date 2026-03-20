using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class HediffDefOf
    {
        public static HediffDef VirtueOfHarvestGod;

        public static HediffDef FourOfAKind;

        public static HediffDef DreamExpress;

        public static HediffDef MindReading;

        public static HediffDef FabledAbodeImmortals;
        
        public static HediffDef SatorisEye;

        public static HediffDef KoishisEye;

        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
    }
}