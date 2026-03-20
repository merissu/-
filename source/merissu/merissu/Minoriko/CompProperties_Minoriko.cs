using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_Minoriko : CompProperties_Rechargeable
    {
        [MustTranslate]
        public string jobString;

        public EffecterDef effectCharged;

        public CompProperties_Minoriko()
        {
            this.compClass = typeof(CompMinoriko);
        }
    }
}