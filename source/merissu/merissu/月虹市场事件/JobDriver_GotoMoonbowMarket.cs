using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobDriver_GotoMoonbowMarket : JobDriver
    {
        protected Pawn Merchant => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Merchant, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil openUI = ToilMaker.MakeToil("OpenMoonbowMarketUI");
            openUI.initAction = delegate
            {
                Pawn actor = openUI.actor;
                if (Merchant != null)
                {
                    Find.WindowStack.Add(new Dialog_MoonbowMarket(actor, Merchant));
                }
            };
            openUI.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return openUI;
        }
    }
}