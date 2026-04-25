using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class CompProperties_AbilityMaoyuDashHit : CompProperties_AbilityEffect
    {
        public int extraPushCells = 0; 
        public DamageDef damageDef = null; 
        public HediffDef speedBuffHediff = null; 

        public CompProperties_AbilityMaoyuDashHit()
        {
            compClass = typeof(CompAbilityEffect_MaoyuDashHit);
        }
    }

    public class CompAbilityEffect_MaoyuDashHit : CompAbilityEffect
    {
        private CompProperties_AbilityMaoyuDashHit Props => (CompProperties_AbilityMaoyuDashHit)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return target.Pawn != null && target.Pawn.Spawned && !target.Pawn.Dead && base.Valid(target, throwMessages);
        }

        public void AddDashSpeedHediff(Pawn pawn)
        {
            if (pawn == null || Props.speedBuffHediff == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(Props.speedBuffHediff) == null)
            {
                pawn.health.AddHediff(Props.speedBuffHediff);
            }
        }

        public void RemoveDashSpeedHediff(Pawn pawn)
        {
            if (pawn == null || Props.speedBuffHediff == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(Props.speedBuffHediff);
            if (h != null) pawn.health.RemoveHediff(h);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || !caster.Spawned || caster.Dead || !victim.Spawned || victim.Dead) return;

            Tool bestTool = null;
            float bestPower = 0f;
            if (caster.def.tools != null)
            {
                foreach (var t in caster.def.tools)
                {
                    if (t != null && t.power > bestPower)
                    {
                        bestPower = t.power;
                        bestTool = t;
                    }
                }
            }

            float damageAmount = Mathf.Max(1f, bestPower);
            DamageDef dmgDef = Props.damageDef ?? DamageDefOf.Stab;
            float armorPen = bestTool?.armorPenetration ?? -1f;

            DamageInfo dinfo = new DamageInfo(dmgDef, damageAmount, armorPen, -1f, caster, null, caster.def);
            if (bestTool != null) dinfo.SetTool(bestTool);
            victim.TakeDamage(dinfo);

            RemoveDashSpeedHediff(caster);

            int bounceDist = Mathf.Max(1, Mathf.RoundToInt(parent.def.verbProperties?.range * 0.5f ?? 2f));
            Vector3 dir = (caster.Position - victim.Position).ToVector3();
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
            dir.Normalize();

            if (TryFindBounceCell(caster, caster.Map, dir, bounceDist, out IntVec3 bounceCell))
            {
                if (caster.jobs?.curJob != null && caster.jobs.curJob.def.driverClass == typeof(JobDriver_CastAbilityDash))
                {
                    caster.jobs.EndCurrentJob(JobCondition.Succeeded, false);
                }

                JumpUtility.DoJump(caster, new LocalTargetInfo(bounceCell), null, parent.def.verbProperties);
            }
        }
        private bool TryFindBounceCell(Pawn pawn, Map map, Vector3 dir, int dist, out IntVec3 result)
        {
            IntVec3 from = pawn.Position;

            for (int d = dist; d >= 1; d--)
            {
                IntVec3 c = from + new IntVec3(Mathf.RoundToInt(dir.x * d), 0, Mathf.RoundToInt(dir.z * d));
                if (c.InBounds(map) && JumpUtility.ValidJumpTarget(pawn, map, c))
                {
                    result = c;
                    return true;
                }
            }

            result = IntVec3.Invalid;
            return false;
        }
    }

    public class JobDriver_CastAbilityDash : JobDriver_CastAbility
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);

            Ability ability = ((Verb_CastAbility)job.verbToUse).ability;
            CompAbilityEffect_MaoyuDashHit comp = ability?.CompOfType<CompAbilityEffect_MaoyuDashHit>();

            AddFinishAction(delegate
            {
                comp?.RemoveDashSpeedHediff(pawn);
            });

            Toil startDash = ToilMaker.MakeToil("StartDash");
            startDash.initAction = delegate
            {
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                comp?.AddDashSpeedHediff(pawn);
            };
            startDash.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return startDash;

            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOn(() => !ability.CanApplyOn(job.targetA));
            gotoToil.tickAction = delegate
            {
                pawn.jobs.curJob.locomotionUrgency = LocomotionUrgency.Sprint;
            };
            yield return gotoToil;

            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }
    }
}