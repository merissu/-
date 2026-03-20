using RimWorld;
using Verse;

namespace merissu
{
    public class CompAbilityEffect_Minoriko : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (parent.pawn != null)
            {
                Hediff firstHediffOfDef = parent.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.VirtueOfHarvestGod);
                if (firstHediffOfDef != null)
                {
                    firstHediffOfDef.Severity = 0f;
                }
            }

            base.Apply(target, dest);

            if (target.HasThing && target.Thing is Plant plant)
            {
                plant.Growth = 1f; 

                plant.Map.mapDrawer.SectionAt(plant.Position).RegenerateAllLayers();
            }
        }
    }
}