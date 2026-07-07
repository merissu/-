using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_PlantWindSway : CompProperties
    {
        public CompProperties_PlantWindSway()
        {
            compClass = typeof(CompPlantWindSway);
        }
    }

    public class CompPlantWindSway : ThingComp
    {
        private Material cachedMat;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            cachedMat = parent.Graphic?.MatAt(parent.Rotation, parent);

            if (cachedMat != null)
            {
                WindManager.Notify_PlantMaterialCreated(cachedMat);
            }
        }
    }
}
