using AM;
using AM.Events;
using AM.Events.Workers;
using RimWorld;
using System.Linq;
using Verse;

namespace merissu.Events
{
    public class CustomDamageWorker : EventWorkerBase
    {
        public override string EventID => "CustomDamage";

        public override void Run(AnimEventInput i)
        {
            var e = i.Event as CustomDamageEvent;
            if (e == null) return;

            Pawn killer = i.GetPawnFromIndex(e.KillerIndex);
            Pawn victim = i.GetPawnFromIndex(e.VictimIndex);
            if (killer == null || victim == null || victim.Dead) return;

            DamageDef damageDef = e.DamageDef.AsDefOfType<DamageDef>(DamageDefOf.Blunt);
            BodyPartDef bodyPartDef = e.TargetBodyPart.AsDefOfType<BodyPartDef>();

            BodyPartRecord bodyPart = victim.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(bp => bp.def == bodyPartDef)
                ?? victim.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(bp => bp.def == BodyPartDefOf.Torso);
            if (bodyPart == null) return;

            var dinfo = new DamageInfo(damageDef, e.DamageAmount, 0f, -1f, killer, bodyPart);
            victim.TakeDamage(dinfo);
        }
    }
}
