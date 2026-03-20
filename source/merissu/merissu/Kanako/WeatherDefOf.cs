using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class WeatherDefOf
    {
        public static WeatherDef SnowGentle;

        public static WeatherDef SnowHard;

        public static WeatherDef FoggyRain;

        public static WeatherDef RainyThunderstorm;

        public static WeatherDef Rain;

        public static WeatherDef Fog;

        static WeatherDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WeatherDefOf));
        }
    }
}