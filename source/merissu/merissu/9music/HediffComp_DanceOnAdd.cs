using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class HediffComp_DanceOnAdd : HediffComp
    {
        private bool triggered;

        public HediffCompProperties_DanceOnAdd Props =>
            (HediffCompProperties_DanceOnAdd)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            if (triggered) return;

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null) return;
            if (pawn.Dead || pawn.Downed) return;

            triggered = true;

            pawn.jobs.StopAll();

            Job danceJob = JobMaker.MakeJob(RimWorld.JobDefOf.Dance);

            danceJob.expiryInterval = Props.danceDurationTicks;
            danceJob.checkOverrideOnExpire = true;

            pawn.jobs.StartJob(
                danceJob,
                JobCondition.InterruptForced,
                null,
                false,
                true
            );
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref triggered, "triggered", false);
        }
    }
}
