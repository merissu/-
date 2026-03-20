using System;
using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_PlayMusicOnce : HediffCompProperties
    {
        public SongDef songDef;

        public HediffCompProperties_PlayMusicOnce()
        {
            compClass = typeof(HediffComp_PlayMusicOnce);
        }
    }
}
