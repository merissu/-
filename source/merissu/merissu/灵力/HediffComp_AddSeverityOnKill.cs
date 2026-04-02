using System;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_AddSeverityOnKill : HediffCompProperties
    {
        public float severityGain = 0.1f;
        public HediffCompProperties_AddSeverityOnKill()
        {
            this.compClass = typeof(HediffComp_AddSeverityOnKill);
        }
    }

    public class HediffComp_AddSeverityOnKill : HediffComp
    {
        public HediffCompProperties_AddSeverityOnKill Props => (HediffCompProperties_AddSeverityOnKill)props;

        public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
        {
            base.Notify_KilledPawn(victim, dinfo);

            if (victim == null) return;

            bool isKiller = false;

            if (dinfo.HasValue)
            {
                if (dinfo.Value.Instigator == Pawn)
                {
                    isKiller = true;
                }
            }

            if (isKiller && victim.HostileTo(Pawn))
            {
                parent.Severity += Props.severityGain;
            }
        }
    }
}