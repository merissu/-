using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace merissu
{
    public class YuukaMod : Mod
    {
        public YuukaMod(ModContentPack content) : base(content)
        {
            new Harmony("merissu.yuukaumbrella").PatchAll();
        }
    }

    public class CompProperties_YuukaUmbrella : CompProperties
    {
        public CompProperties_YuukaUmbrella() { this.compClass = typeof(CompYuukaUmbrella); }
    }

    public class CompYuukaUmbrella : ThingComp
    {
        private static readonly string HediffDefName = "Openumbrella";
        private Pawn HoldingPawn => (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        public void RefreshPawnStatus()
        {
            Pawn pawn = HoldingPawn;
            if (pawn == null || pawn.Dead) return;

            bool isOpenedDef = parent.def.defName == "OpenYuukaUmbrella";
            HediffDef umbrellaHediff = HediffDef.Named(HediffDefName);

            if (isOpenedDef && pawn.equipment?.Primary == parent)
            {
                if (!pawn.health.hediffSet.HasHediff(umbrellaHediff))
                    pawn.health.AddHediff(umbrellaHediff);
            }
            else
            {
                RemoveHediffFromPawn(pawn);
            }
        }

        private void RemoveHediffFromPawn(Pawn pawn)
        {
            HediffDef h = HediffDef.Named(HediffDefName);
            Hediff first = pawn.health.hediffSet.GetFirstHediffOfDef(h);
            if (first != null) pawn.health.RemoveHediff(first);
        }

        public override void CompTickRare() { base.CompTickRare(); RefreshPawnStatus(); }
        public override void Notify_Equipped(Pawn pawn) { base.Notify_Equipped(pawn); RefreshPawnStatus(); }
        public override void Notify_Unequipped(Pawn pawn) { base.Notify_Unequipped(pawn); RemoveHediffFromPawn(pawn); }
    }

    public class CompProperties_AbilityOpenUmbrella : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityOpenUmbrella()
        {
            compClass = typeof(CompAbility_OpenUmbrella);
        }
    }
    public class CompAbility_OpenUmbrella : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn?.equipment?.Primary == null) return;

            ThingWithComps oldWeapon = pawn.equipment.Primary;
            ThingDef newDef = null;

            if (oldWeapon.def.defName == "YuukaUmbrella")
                newDef = DefDatabase<ThingDef>.GetNamed("OpenYuukaUmbrella");
            else if (oldWeapon.def.defName == "OpenYuukaUmbrella")
                newDef = DefDatabase<ThingDef>.GetNamed("YuukaUmbrella");

            if (newDef != null)
            {
                ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(newDef, oldWeapon.Stuff);

                newWeapon.HitPoints = oldWeapon.HitPoints; 

                CompQuality oldQuality = oldWeapon.TryGetComp<CompQuality>();
                CompQuality newQuality = newWeapon.TryGetComp<CompQuality>();
                if (oldQuality != null && newQuality != null)
                {
                    newQuality.SetQuality(oldQuality.Quality, ArtGenerationContext.Colony);
                }
                var oldBiocode = oldWeapon.TryGetComp<CompBiocodable>();
                if (oldBiocode != null && oldBiocode.Biocoded)
                    newWeapon.TryGetComp<CompBiocodable>()?.CodeFor(oldBiocode.CodedPawn);

                pawn.equipment.Remove(oldWeapon);
                oldWeapon.Destroy();
                pawn.equipment.AddEquipment(newWeapon);

                pawn.Drawer.renderer.SetAllGraphicsDirty();
                bool isOpening = newDef.defName == "OpenYuukaUmbrella";
                Messages.Message(isOpening ? "展开了阳伞。" : "收起了阳伞。",
                    isOpening ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}