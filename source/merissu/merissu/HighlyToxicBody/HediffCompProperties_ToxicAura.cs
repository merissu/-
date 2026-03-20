using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_ToxicAura : HediffCompProperties
    {
        public int applyIntervalTicks;
        public int radius;

        public HediffDef toxicHediff;
        public float toxicSeverity;

        public bool causeBerserk;

        // ★ 音乐
        public SongDef songDef;

        public HediffCompProperties_ToxicAura()
        {
            compClass = typeof(HediffComp_ToxicAura);
        }
    }
}
