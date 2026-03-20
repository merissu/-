using Verse;

namespace merissu
{
    public class HediffCompProperties_DanceOnAdd : HediffCompProperties
    {
        // 跳舞持续时间（tick）
        public int danceDurationTicks = 300;

        public HediffCompProperties_DanceOnAdd()
        {
            compClass = typeof(HediffComp_DanceOnAdd);
        }
    }
}
