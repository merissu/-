using Verse;
using RimWorld;
using System.Collections.Generic; 
using System.Linq; 

namespace merissu
{
    public class HediffCompProperties_DamageOnDowned : HediffCompProperties
    {
        public HediffCompProperties_DamageOnDowned()
        {
            this.compClass = typeof(HediffComp_DamageOnDowned);
        }
    }

    public class HediffComp_DamageOnDowned : HediffComp
    {
        private bool hasAppliedDamage = false;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(20))
            {
                if (Pawn.Downed)
                {
                    if (!hasAppliedDamage)
                    {
                        ApplyTotalFrostbiteDamage(); 
                        hasAppliedDamage = true;
                    }
                }
                else
                {
                    hasAppliedDamage = false;
                }
            }
        }

        private void ApplyTotalFrostbiteDamage()
        {
            float totalDamageToApply = 50f; 
            DamageDef damageDef = DamageDefOf.Frostbite;

            List<BodyPartRecord> limbs = Pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore) ||
                               part.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore))
                .ToList();

            if (limbs.NullOrEmpty())
            {
                return;
            }

            while (totalDamageToApply > 0f && !Pawn.Dead)
            {
                BodyPartRecord targetPart = limbs.RandomElement();

                float damageThisTime = Rand.Range(5f, 10f);
                damageThisTime = System.Math.Min(damageThisTime, totalDamageToApply);

                DamageInfo dinfo = new DamageInfo(damageDef, damageThisTime, 0f, -1f, null, targetPart);

                Pawn.TakeDamage(dinfo);

                totalDamageToApply -= damageThisTime;

                if (Pawn.Dead) break;
            }

        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref hasAppliedDamage, "hasAppliedDamage", false);
        }
    }
}