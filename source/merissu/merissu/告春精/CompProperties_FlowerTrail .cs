using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class CompProperties_FlowerTrail : CompProperties
    {
        public int checkInterval = 10;
        public float spawnChance = 1.0f; 
        public bool checkTemperature = false;
        public List<ThingDef> flowers;
        public int maxFlowers = 100;
        public int maxFailures = 50; 

        public CompProperties_FlowerTrail()
        {
            compClass = typeof(CompFlowerTrail);
        }
    }

    public class CompFlowerTrail : ThingComp
    {
        private IntVec3 lastPos = IntVec3.Invalid;
        private int flowerCount = 0;
        private int failureCount = 0; 
        private bool isLeaving = false; 

        public CompProperties_FlowerTrail Props => (CompProperties_FlowerTrail)props;

        public override void CompTick()
        {
            if (!parent.Spawned || parent.Map == null) return;

            if (isLeaving)
            {
                if (parent.IsHashIntervalTick(100))
                {
                    Pawn pawn = parent as Pawn;
                    if (pawn != null && !pawn.Dead)
                    {
                        if (pawn.CurJob == null || pawn.CurJob.def == RimWorld.JobDefOf.Wait_Wander || pawn.CurJob.def == RimWorld.JobDefOf.GotoWander)
                        {
                            IntVec3 exitSpot;
                            if (RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
                            {
                                Job job = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, exitSpot);
                                job.exitMapOnArrival = true;
                                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                            }
                            else
                            {
                                pawn.DeSpawn();
                            }
                        }
                    }
                }
                return;
            }

            if (!parent.IsHashIntervalTick(Props.checkInterval)) return;
            IntVec3 cur = parent.Position;
            if (!lastPos.IsValid)
            {
                lastPos = cur;
                return;
            }

            if (cur != lastPos)
            {
                if (TrySpawnFlowerAt(lastPos))
                {
                    failureCount = 0; 
                }
                else
                {
                    failureCount++; 
                }
                lastPos = cur;
            }

            CheckExitConditions();
        }

        private bool TrySpawnFlowerAt(IntVec3 cell)
        {
            Map map = parent.Map;
            if (!cell.InBounds(map)) return false;
            if (Props.flowers == null || Props.flowers.Count == 0) return false;

            if (cell.GetPlant(map) != null) return false;

            if (!Rand.Chance(Props.spawnChance)) return false;

            ThingDef flower = Props.flowers.RandomElement();
            if (flower?.plant == null) return false;

            if (flower.CanEverPlantAt(cell, map, false, Props.checkTemperature))
            {
                GenSpawn.Spawn(flower, cell, map);
                flowerCount++;
                return true;
            }
            return false;
        }

        private void CheckExitConditions()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            if (flowerCount >= Props.maxFlowers)
            {
                StartLeaving(pawn, "已经宣告完春天的到来，准备离开了。");
                return;
            }

            if (failureCount >= Props.maxFailures)
            {
                StartLeaving(pawn, "找不到可以散播春天的地方，直接离开了。");
            }
        }

        private void StartLeaving(Pawn pawn, string reason)
        {
            if (pawn.Faction == Faction.OfPlayer) return;

            isLeaving = true;
            Messages.Message(pawn.LabelShort + " " + reason, pawn, MessageTypeDefOf.NeutralEvent);

            IntVec3 exitSpot;
            if (RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
            {
                Job job = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, exitSpot);

                job.exitMapOnArrival = true;

                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            else
            {
                pawn.DeSpawn();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref flowerCount, "flowerCount", 0);
            Scribe_Values.Look(ref failureCount, "failureCount", 0);
            Scribe_Values.Look(ref isLeaving, "isLeaving", false);
        }
    }
}