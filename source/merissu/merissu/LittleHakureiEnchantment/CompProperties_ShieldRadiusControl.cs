using Verse;

namespace merissu
{
    public class CompProperties_ShieldRadiusControl : CompProperties
    {
        public int minRadius = 5;
        public int maxRadius = 60;
        public int step = 5;

        public CompProperties_ShieldRadiusControl()
        {
            compClass = typeof(Comp_ShieldRadiusControl);
        }
    }
}
