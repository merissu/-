using RimWorld;
using Verse;

namespace merissu
{
    public class HediffComp_PlayMusicOnce : HediffComp
    {
        public HediffCompProperties_PlayMusicOnce Props =>
            (HediffCompProperties_PlayMusicOnce)props;

        public override void CompPostMake()
        {
            base.CompPostMake();

            if (Props.songDef != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(Props.songDef, false);
            }
        }
    }
}
