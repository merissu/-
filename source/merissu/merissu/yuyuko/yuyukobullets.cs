using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace merissu
{
    public class yuyukobullets : Bullet
    {
        protected override void Tick()
        {
            base.Tick();
            if (Destroyed) return;
            CheckAdvancedCollision();
        }

        private void CheckAdvancedCollision()
        {
            Vector3 currentPos = DrawPos;
            IntVec3 intPos = currentPos.ToIntVec3();

            if (!intPos.InBounds(Map)) return;

            IEnumerable<Thing> list = GenRadial.RadialDistinctThingsAround(
                intPos,
                Map,
                0.5f,
                true
            );

            foreach (Thing thing in list)
            {
                if (thing == launcher) continue;

                if (thing is Pawn p && !p.Dead && p.Faction != launcher.Faction)
                {
                    ApplyYuyukoDamage(p);
                    this.Destroy(); 
                    return;
                }
                else if (thing is Building b)
                {
                    this.Impact(b);
                    return;
                }
            }
        }

        private void ApplyYuyukoDamage(Pawn victim)
        {
            float damageAmount = this.def.projectile.GetDamageAmount(launcher);
            DamageDef customDmgDef = DefDatabase<DamageDef>.GetNamed("DeathButterflyFloatingMoon");

            DamageInfo dinfo = new DamageInfo(
                customDmgDef,
                damageAmount,
                9999f, 
                -1f,
                this.launcher,
                null,
                this.def
            );

            dinfo.SetIgnoreArmor(true);
            dinfo.SetIgnoreInstantKillProtection(true);

            DamageWorker.DamageResult result = victim.TakeDamage(dinfo);

            if (result.totalDamageDealt <= 0.01f && !victim.Dead)
            {
                BodyPartRecord part = victim.health.hediffSet.GetNotMissingParts().RandomElement();
                Hediff_Injury injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDef.Named("DeathReturns"), victim, part);
                injury.Severity = damageAmount;
                victim.health.AddHediff(injury, part, dinfo);

                float extraSeverity = damageAmount * 0.2f;
                HealthUtility.AdjustSeverity(victim, HediffDef.Named("EternalSleepInFantasy"), extraSeverity);
            }
        }
    }
}