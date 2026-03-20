using System.Collections.Generic;
using RimWorld;
using Verse;

namespace merissu
{
    public class CompEquippableMultiAbility : CompEquippable
    {
        private Dictionary<AbilityDef, int> cooldowns = new Dictionary<AbilityDef, int>();

        public CompProperties_EquippableMultiAbility Props => (CompProperties_EquippableMultiAbility)props;

        public Pawn GetPawn => (parent?.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        public void UpdateAbilities(Pawn pawn)
        {
            if (pawn == null || pawn.abilities == null || Props.abilityDefs == null) return;

            bool changed = false;
            foreach (var def in Props.abilityDefs)
            {
                if (def == null) continue;

                Ability ab = pawn.abilities.GetAbility(def);
                if (ab == null)
                {
                    pawn.abilities.GainAbility(def);
                    ab = pawn.abilities.GetAbility(def);

                    if (ab != null && cooldowns.TryGetValue(def, out int ticks) && ticks > 0)
                    {
                        ab.StartCooldown(ticks);
                    }
                    changed = true;
                }
            }
            if (changed) pawn.abilities.Notify_TemporaryAbilitiesChanged();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            UpdateAbilities(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);

            if (pawn?.abilities == null) return;

            foreach (var def in Props.abilityDefs)
            {
                Ability ab = pawn.abilities.GetAbility(def);
                if (ab != null)
                {
                    cooldowns[def] = ab.CooldownTicksRemaining;
                    pawn.abilities.RemoveAbility(def);
                }
            }
            pawn.abilities.Notify_TemporaryAbilitiesChanged();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref cooldowns, "cooldowns", LookMode.Def, LookMode.Value);
            if (cooldowns == null) cooldowns = new Dictionary<AbilityDef, int>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Pawn pawn = GetPawn;
                if (pawn != null && pawn.abilities != null)
                {
                    pawn.abilities.abilities.RemoveAll(a => a == null || a.def == null);
                    UpdateAbilities(pawn);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(250))
            {
                UpdateAbilities(GetPawn);
            }
        }
    }
}