using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_Usable_CustomIcon : CompProperties_Usable
    {
        // XML 里填的贴图路径
        public string iconPath;

        public CompProperties_Usable_CustomIcon()
        {
            this.compClass = typeof(CompUsable_CustomIcon);
        }
    }
}
