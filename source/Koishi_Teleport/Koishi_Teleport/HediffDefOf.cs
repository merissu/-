using RimWorld;
using Verse;

namespace THSIR_KoishiTeleport
{
    [DefOf]
    public static class HediffDefOf
    {
        public static HediffDef TheLiberationOfTheId;

        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
    }
}
