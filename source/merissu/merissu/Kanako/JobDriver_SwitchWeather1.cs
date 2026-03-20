using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobDriver_SwitchWeather1 : JobDriver
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
            WeatherDef weather = RimWorld.WeatherDefOf.Clear;

            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(
                TargetIndex.A, PathEndMode.InteractionCell);

            yield return Toils_General.Wait(0.55f.SecondsToTicks());

            Toil toil = Toils_General.Wait(1.5f.SecondsToTicks());
            toil.WithEffect(
                () => EffecterDefOf.MinorikoUse,
                job.GetTarget(TargetIndex.A).Thing);
            yield return toil;

            yield return Toils_General.Do(delegate
            {
                Thing thing = job.GetTarget(TargetIndex.A).Thing;

                thing?.TryGetComp<CompRechargeable>()?.Discharge();

                if (pawn.Map != null)
                {
                    pawn.Map.weatherManager.TransitionTo(weather);
                }

                CompSwitchWeather comp =
                    thing?.TryGetComp<CompSwitchWeather>();
                if (comp != null)
                {
                    comp.Notify_Used();
                }
            });

            yield return Toils_General.Wait(0.35f.SecondsToTicks());
        }
    }
}
