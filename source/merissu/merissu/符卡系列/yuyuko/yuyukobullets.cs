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
                    if (b.def.fillPercent > 0)
                    {
                        if (b is Building_Turret && b.Faction == launcher?.Faction) continue;

                        this.Impact(b);
                        return;
                    }
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

            if (!victim.Dead)
            {
                if (victim.health.hediffSet.GetNotMissingParts().TryRandomElement(out BodyPartRecord part))
                {
                    Hediff_Injury injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDef.Named("DeathReturns"), victim, part);
                    injury.Severity = damageAmount;
                    victim.health.AddHediff(injury, part, dinfo);
                }

                victim.Drawer?.Notify_DamageApplied(dinfo);

                if (dinfo.Def.impactSoundType != null)
                {
                    ImpactSoundUtility.PlayImpactSound(victim, dinfo.Def.impactSoundType, victim.Map);
                }

                victim.mindState?.Notify_DamageTaken(dinfo);
                if (victim.Faction != null && launcher != null && launcher.Faction != null)
                {
                    victim.Faction.Notify_MemberTookDamage(victim, dinfo);
                }

                if (victim.stances != null)
                {
                    if (victim.stances.stunner != null)
                    {
                        victim.stances.stunner.StunFor(95, this.launcher);
                    }

                    if (victim.stances.curStance is Stance_Busy)
                    {
                        victim.stances.SetStance(new Stance_Mobile());
                    }
                }
            }
        }
    }
}