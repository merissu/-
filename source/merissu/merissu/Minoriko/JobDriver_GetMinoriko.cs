using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobDriver_GetMinoriko : JobDriver
    {
        private const TargetIndex ChargerInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(
                job.GetTarget(TargetIndex.A).Thing,
                job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(
                TargetIndex.A, PathEndMode.InteractionCell);

            yield return Toils_General.Wait(0.55f.SecondsToTicks());

            Toil useEffect = Toils_General.Wait(1.5f.SecondsToTicks());
            useEffect.WithEffect(
                () => EffecterDefOf.MinorikoUse,
                job.GetTarget(TargetIndex.A).Thing);
            yield return useEffect;

            yield return Toils_General.Do(delegate
            {
                Thing thing = job.GetTarget(TargetIndex.A).Thing;

                thing?.TryGetComp<CompRechargeable>()?.Discharge();

                pawn.health.AddHediff(HediffDefOf.VirtueOfHarvestGod);

                CompMinoriko comp = thing?.TryGetComp<CompMinoriko>();
                if (comp != null)
                {
                    comp.Notify_Used();
                }
            });

            yield return Toils_General.Wait(0.35f.SecondsToTicks());
        }
    }
}
