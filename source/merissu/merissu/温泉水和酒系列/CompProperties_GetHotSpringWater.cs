using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class CompProperties_GetHotSpringWater : CompProperties
    {
        public string waterThingDef;

        public int waterAmount = 1;

        public int workTicks = 300;

        public CompProperties_GetHotSpringWater()
        {
            compClass = typeof(CompGetHotSpringWater);
        }
    }

    public class CompGetHotSpringWater : ThingComp
    {
        public CompProperties_GetHotSpringWater Props =>
            (CompProperties_GetHotSpringWater)props;
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var option in base.CompFloatMenuOptions(selPawn))
                yield return option;

            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
                yield break;

            yield return new FloatMenuOption(
                "获取温泉水",
                delegate
                {
                    StartCollectJob(selPawn);
                });
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            yield return new Command_Action
            {
                defaultLabel = "获取温泉水",
                defaultDesc = "指定一名殖民者收集温泉水",

                icon = ContentFinder<Texture2D>.Get(
                    "Medicine/GeyserWater",
                    false),

                action = delegate
                {
                    Find.Targeter.BeginTargeting(
                        new TargetingParameters
                        {
                            canTargetPawns = true,
                            canTargetBuildings = false,
                            canTargetItems = false,
                            canTargetAnimals = false,
                            canTargetMechs = false,

                            validator = delegate (TargetInfo target)
                            {
                                Pawn pawn = target.Thing as Pawn;

                                return pawn != null
                                    && pawn.IsColonistPlayerControlled;
                            }
                        },
                        delegate (LocalTargetInfo target)
                        {
                            Pawn pawn = target.Pawn;

                            if (pawn == null)
                                return;

                            StartCollectJob(pawn);
                        });
                }
            };
        }

        private void StartCollectJob(Pawn pawn)
        {
            if (pawn == null)
                return;

            if (!pawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
            {
                Messages.Message(
                    "无法到达温泉机",
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            Job job = JobMaker.MakeJob(
                DefDatabase<JobDef>.GetNamed("CollectHotSpringWater"),
                parent);

            job.playerForced = true;

            pawn.jobs.TryTakeOrderedJob(job);

            Messages.Message(
                pawn.LabelShort + "开始收集温泉水",
                MessageTypeDefOf.TaskCompletion,
                false);
        }
    }
}