using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class CompMinoriko : ThingComp
    {
        private int lastUseTick = -999999;

        public CompNeuralSupercharger.AutoUseMode autoUseMode =
            CompNeuralSupercharger.AutoUseMode.AutoUseWithDesire;

        public bool allowGuests;

        private CompProperties_Minoriko Props =>
            (CompProperties_Minoriko)props;

        private bool CanUseToday
        {
            get
            {
                int ticksPassed = Find.TickManager.TicksGame - lastUseTick;
                return ticksPassed >= GenDate.TicksPerDay; // 60000
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            autoUseMode = CompNeuralSupercharger.AutoUseMode.AutoUseWithDesire;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref autoUseMode, "autoUseMode",
                CompNeuralSupercharger.AutoUseMode.AutoUseWithDesire);
            Scribe_Values.Look(ref allowGuests, "allowGuests", false);
            Scribe_Values.Look(ref lastUseTick, "lastUseTick", -999999);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!CanUseToday)
            {
                int ticksLeft =
                    GenDate.TicksPerDay -
                    (Find.TickManager.TicksGame - lastUseTick);

                string timeLeft = ticksLeft.ToStringTicksToPeriod();

                yield return new FloatMenuOption(
                    $"{Props.jobString}（今日已使用，剩余 {timeLeft}）",
                    null);
                yield break;
            }

            if (selPawn.CurJob != null &&
                selPawn.CurJob.def == JobDefOf.GetMinoriko &&
                selPawn.CurJob.targetA.Thing == parent)
            {
                yield return new FloatMenuOption(
                    Props.jobString + "（正在使用中）",
                    null);
                yield break;
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(Props.jobString, delegate
                {
                    Job job = JobMaker.MakeJob(JobDefOf.GetMinoriko, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                selPawn, parent);
        }

        public void Notify_Used()
        {
            lastUseTick = Find.TickManager.TicksGame;
        }
    }
}
