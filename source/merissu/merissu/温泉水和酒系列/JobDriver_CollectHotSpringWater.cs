using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobDriver_CollectHotSpringWater : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

    protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(
                TargetIndex.A,
                PathEndMode.Touch);

            CompGetHotSpringWater comp =
                TargetThingA.TryGetComp<CompGetHotSpringWater>();

            int duration = comp?.Props.workTicks ?? 300;

            Toil collect = ToilMaker.MakeToil();

            collect.defaultCompleteMode =
                ToilCompleteMode.Never;

            int tickCounter = 0;

            collect.WithProgressBar(
                TargetIndex.A,
                () => (float)tickCounter / duration);

            collect.tickAction = delegate
            {
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.rotationTracker?.FaceTarget(TargetA);

                tickCounter++;

                if (tickCounter >= duration)
                {
                    tickCounter = 0;

                    ThingDef waterDef =
                        DefDatabase<ThingDef>.GetNamed(
                            comp.Props.waterThingDef);

                    Thing water =
                        ThingMaker.MakeThing(waterDef);

                    water.stackCount =
                        comp.Props.waterAmount;

                    GenPlace.TryPlaceThing(
                        water,
                        pawn.Position,
                        pawn.Map,
                        ThingPlaceMode.Near);
                }
            };

            yield return collect;
        }
    }
}
