using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace meriss
{
    public class CompProperties_PlantableMeriss : CompProperties
    {
        public ThingDef plantDefToSpawn;

        public CompProperties_PlantableMeriss()
        {
            compClass = typeof(CompPlantableMeriss);
        }
    }

    public class CompPlantableMeriss : ThingComp
    {
        private static TargetingParameters TargetingParams => new TargetingParameters
        {
            canTargetLocations = true,
            canTargetPawns = false
        };

        public CompProperties_PlantableMeriss Props =>
            (CompProperties_PlantableMeriss)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "种植 " + Props.plantDefToSpawn.label,
                defaultDesc = "在选定位置种植 " + Props.plantDefToSpawn.label,
                icon = Props.plantDefToSpawn.uiIcon,
                action = BeginTargeting
            };
        }

        private void BeginTargeting()
        {
            Find.Targeter.BeginTargeting(TargetingParams, delegate (LocalTargetInfo target)
            {
                IntVec3 cell = target.Cell;
                Map map = parent.MapHeld;

                if (!cell.IsValid || cell.Fogged(map))
                    return;

                Thing blockingThing;
                AcceptanceReport report =
                    Props.plantDefToSpawn.CanEverPlantAt(cell, map, out blockingThing);

                if (!report.Accepted)
                {
                    if (!report.Reason.NullOrEmpty())
                    {
                        Messages.Message(
                            report.Reason,
                            parent,
                            MessageTypeDefOf.RejectInput);
                    }
                    return;
                }

                Plant plant = (Plant)ThingMaker.MakeThing(Props.plantDefToSpawn);

                if (GenPlace.TryPlaceThing(plant, cell, map, ThingPlaceMode.Direct))
                {
                    plant.Growth = 0.0001f;
                    plant.sown = true;

                    parent.stackCount--;

                    if (parent.stackCount <= 0)
                        parent.Destroy();

                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }

            }, null, null, null, null, null, Props.plantDefToSpawn.uiIcon);
        }
    }
}