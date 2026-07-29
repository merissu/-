using Verse;
using RimWorld;

namespace merissu
{
    public class CompProperties_ToggleHakkeroMode : CompProperties_AbilityEffect
    {
        public CompProperties_ToggleHakkeroMode()
        {
            this.compClass = typeof(CompAbility_ToggleHakkeroMode);
        }
    }

    public class CompAbility_ToggleHakkeroMode : CompAbilityEffect
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
            string message = null;
            MessageTypeDef messageType = MessageTypeDefOf.NeutralEvent;

            if (oldWeapon.def.defName == "Hakkero")
            {
                newDef = DefDatabase<ThingDef>.GetNamed("HakkeroLaser");
                message = "切换为弹幕模式";
                messageType = MessageTypeDefOf.CautionInput;
            }
            else if (oldWeapon.def.defName == "HakkeroLaser")
            {
                newDef = DefDatabase<ThingDef>.GetNamed("Hakkerobiglaser");
                message = "切换为激光模式";
                messageType = MessageTypeDefOf.CautionInput;
            }
            else if (oldWeapon.def.defName == "Hakkerobiglaser")
            {
                newDef = DefDatabase<ThingDef>.GetNamed("Hakkero");
                message = "八卦炉切换为火焰喷射模式";
                messageType = MessageTypeDefOf.NeutralEvent;
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

            if (!message.NullOrEmpty())
            {
                Messages.Message(message, messageType);
            }
        }
    }
}