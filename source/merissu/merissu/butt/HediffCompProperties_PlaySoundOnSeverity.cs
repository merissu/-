using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_PlaySoundOnSeverity : HediffCompProperties
    {
        public float severityThreshold = 0.8f;
        public SoundDef soundDef;

        public HediffCompProperties_PlaySoundOnSeverity()
        {
            this.compClass = typeof(HediffComp_PlaySoundOnSeverity);
        }
    }
}
