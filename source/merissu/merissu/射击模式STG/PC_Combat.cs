using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace merissu
{
    public partial class PC
    {
        private Verb GetActiveVerb()
        {
            var verb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
            if (verb == null || verb.verbProps.IsMeleeAttack)
            {
                verb = pawn.VerbTracker?.AllVerbs?
                    .FirstOrDefault(v => v is Verb_MeleeAttack && v.Available());
            }
            return verb;
        }

        private bool IsAbilityCastJob()
        {
            return pawn.CurJob?.ability != null;
        }
    }
}