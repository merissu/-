using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class CompAbilityEffect_GiveRandomInspiration : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.mindState?.inspirationHandler == null)
                return;

            var handler = pawn.mindState.inspirationHandler;

            List<InspirationDef> inspirations = DefDatabase<InspirationDef>
                .AllDefs
                .Where(def =>
                    def.Worker != null &&
                    def.Worker.InspirationCanOccur(pawn))
                .ToList();

            if (inspirations.NullOrEmpty())
                return;

            InspirationDef chosen = inspirations.RandomElement();

            handler.TryStartInspiration(chosen);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return parent?.pawn?.mindState?.inspirationHandler != null;
        }
    }
}
