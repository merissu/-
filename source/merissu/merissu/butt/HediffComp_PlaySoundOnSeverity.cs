using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class HediffComp_PlaySoundOnSeverity : HediffComp
    {
        private bool triggered = false;

        public HediffCompProperties_PlaySoundOnSeverity Props =>
            (HediffCompProperties_PlaySoundOnSeverity)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (triggered) return;

            if (this.parent.Severity >= Props.severityThreshold)
            {
                Pawn pawn = this.parent.pawn;
                if (pawn?.Map != null)
                {
                    Props.soundDef?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }

                triggered = true;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref triggered, "triggered", false);
        }
    }
}
