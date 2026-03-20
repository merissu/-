using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class CompSwitchWeather : ThingComp
    {
        private int lastUseTick = -999999;

        private bool CanUseToday
        {
            get
            {
                return Find.TickManager.TicksGame - lastUseTick
                       >= GenDate.TicksPerDay; // 60000
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
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
                    $"今日已使用（剩余 {timeLeft}）",
                    null);
                yield break;
            }

            yield return MakeOption("把天气改为晴天", JobDefOf.SwitchWeather1, selPawn);
            yield return MakeOption("把天气改为雨天", JobDefOf.SwitchWeather2, selPawn);
            yield return MakeOption("把天气改为雾天", JobDefOf.SwitchWeather3, selPawn);
            yield return MakeOption("把天气改为暴风雨", JobDefOf.SwitchWeather4, selPawn);
            yield return MakeOption("把天气改为大雪天", JobDefOf.SwitchWeather5, selPawn);
            yield return MakeOption("把天气改为小雪天", JobDefOf.SwitchWeather6, selPawn);
            yield return MakeOption("把天气改为雾雨天", JobDefOf.SwitchWeather7, selPawn);
        }

        private FloatMenuOption MakeOption(
            string label, JobDef jobDef, Pawn pawn)
        {
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, delegate
                {
                    Job job = JobMaker.MakeJob(jobDef, parent);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                pawn, parent);
        }

        public void Notify_Used()
        {
            lastUseTick = Find.TickManager.TicksGame;
        }
    }
}
