using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class EffecterDefOf
    {
        public static EffecterDef MinorikoUse;

        static EffecterDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EffecterDefOf));
        }
    }
}