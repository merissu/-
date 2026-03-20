using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class JobDefOf
    {
        public static JobDef GetMinoriko;

        public static JobDef SwitchWeather1;
        public static JobDef SwitchWeather2;
        public static JobDef SwitchWeather3;
        public static JobDef SwitchWeather4;
        public static JobDef SwitchWeather5;
        public static JobDef SwitchWeather6;
        public static JobDef SwitchWeather7;

        static JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
    }
}