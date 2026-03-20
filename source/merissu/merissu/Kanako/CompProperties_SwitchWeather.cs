using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_SwitchWeather : CompProperties_Rechargeable
    {
        [MustTranslate]
        public string jobString; 

        public EffecterDef effectCharged; 

        public CompProperties_SwitchWeather()
        {
            this.compClass = typeof(CompSwitchWeather);
        }
    }
}