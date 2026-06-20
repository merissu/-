using Verse;
using RimWorld;

namespace merissu
{
    public class CompProperties_ToggleNuclearMode : CompProperties_AbilityEffect
    {
        public CompProperties_ToggleNuclearMode()
        {
            this.compClass = typeof(CompAbility_ToggleNuclearMode);
        }
    }

    public class CompAbility_ToggleNuclearMode : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn pawn = parent.pawn;
            if (pawn == null) return;

            var equipment = pawn.equipment;
            if (equipment == null) return;

            ThingWithComps oldWeapon = equipment.Primary;
            if (oldWeapon == null) return;

            ThingDef newDef = null;

            if (oldWeapon.def.defName == "NuclearControlRod")
            {
                newDef = DefDatabase<ThingDef>.GetNamed("BoomNuclearControlRod");
            }
            else if (oldWeapon.def.defName == "BoomNuclearControlRod")
            {
                newDef = DefDatabase<ThingDef>.GetNamed("NuclearControlRod");
            }
            else
            {
                return; 
            }

            ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(newDef);

            newWeapon.HitPoints = oldWeapon.HitPoints;

            CompQuality oldQuality = oldWeapon.TryGetComp<CompQuality>();
            CompQuality newQuality = newWeapon.TryGetComp<CompQuality>();
            if (oldQuality != null && newQuality != null)
            {
                newQuality.SetQuality(oldQuality.Quality, ArtGenerationContext.Colony);
            }

            equipment.Remove(oldWeapon);
            oldWeapon.Destroy();

            equipment.AddEquipment(newWeapon);

            if (newDef.defName == "BoomNuclearControlRod")
            {
                Messages.Message("制御棒聚变模式", MessageTypeDefOf.CautionInput);
            }
            else
            {
                Messages.Message("制御棒激光模式", MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}