using System.Collections.Generic;
using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_EquippableMultiAbility : CompProperties
    {
        public List<AbilityDef> abilityDefs;

        public CompProperties_EquippableMultiAbility()
        {
            compClass = typeof(CompEquippableMultiAbility);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (abilityDefs == null || abilityDefs.Count == 0)
            {
                yield return parentDef.defName + " has CompEquippableMultiAbility but no abilityDefs defined.";
            }
        }
    }
}