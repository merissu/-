using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobGiver_YinYangFirebeam : JobGiver_AIFightEnemy
    {
        public float retreatRatio = 0.30f;     
        public float meleeRatio = 0.10f;       
        public float retreatBandTolerance = 0.25f; 

        private const int TargetUpdateInterval = 15;
        private const int RetreatSearchInterval = 30;
        private const int CastPosSearchInterval = 20;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null || enemyTarget.Destroyed || pawn.IsHashIntervalTick(TargetUpdateInterval))
            {
                UpdateEnemyTarget(pawn);
                enemyTarget = pawn.mindState.enemyTarget;
            }

            if (enemyTarget == null || enemyTarget.Destroyed)
                return null;

            Verb rangedVerb = GetLongestRangedVerb(pawn, out float maxAttackRange);
            if (rangedVerb == null || maxAttackRange <= 0.1f)
            {
                return MeleeAttackJob(pawn, enemyTarget);
            }

            float distSq = (pawn.Position - enemyTarget.Position).LengthHorizontalSquared;
            float attackSq = maxAttackRange * maxAttackRange;

            float retreatDist = maxAttackRange * Mathf.Max(0.01f, retreatRatio);
            float meleeDist = maxAttackRange * Mathf.Clamp(meleeRatio, 0f, retreatRatio);

            float retreatSq = retreatDist * retreatDist;
            float meleeSq = meleeDist * meleeDist;

            if (distSq <= meleeSq)
            {
                Job melee = MeleeAttackJob(pawn, enemyTarget);
                if (melee != null) return melee;
            }

            if (distSq > meleeSq && distSq < retreatSq && pawn.IsHashIntervalTick(RetreatSearchInterval))
            {
                if (TryFindRetreatCell(pawn, enemyTarget, distSq, retreatDist, out IntVec3 retreatCell))
                {
                    Job retreat = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, retreatCell);
                    retreat.expiryInterval = 90;
                    retreat.checkOverrideOnExpire = true;
                    return retreat;
                }
            }

            if (distSq <= attackSq && rangedVerb.CanHitTarget(enemyTarget))
            {
                Job shoot = JobMaker.MakeJob(RimWorld.JobDefOf.UseVerbOnThing, enemyTarget);
                shoot.verbToUse = rangedVerb;
                return shoot;
            }

            if (distSq > attackSq && pawn.IsHashIntervalTick(CastPosSearchInterval))
            {
                if (TryFindShootingPosition(pawn, out IntVec3 dest, rangedVerb, maxAttackRange))
                {
                    Job move = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, dest);
                    move.expiryInterval = 120;
                    move.checkOverrideOnExpire = true;
                    return move;
                }
            }

            return JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, 30, true);
        }

        private Verb GetLongestRangedVerb(Pawn pawn, out float maxRange)
        {
            maxRange = 0f;
            Verb best = null;

            var verbs = pawn.verbTracker?.AllVerbs;
            if (verbs == null) return null;

            for (int i = 0; i < verbs.Count; i++)
            {
                Verb v = verbs[i];
                if (v == null || v.IsMeleeAttack || !v.Available()) continue;

                float r = v.verbProps?.range ?? 0f;
                if (r > maxRange)
                {
                    maxRange = r;
                    best = v;
                }
            }

            return best;
        }

        private bool TryFindRetreatCell(Pawn pawn, Thing enemyTarget, float currentDistSq, float retreatDist, out IntVec3 retreatCell)
        {
            int radius = Mathf.Clamp(Mathf.CeilToInt(retreatDist), 4, 12);
            float desiredSq = retreatDist * retreatDist;
            float minSq = desiredSq * (1f - retreatBandTolerance);
            float maxSq = desiredSq * (1f + retreatBandTolerance);

            retreatCell = CellFinder.RandomClosewalkCellNear(
                pawn.Position,
                pawn.Map,
                radius,
                c =>
                {
                    if (!c.IsValid || c == pawn.Position || !c.Standable(pawn.Map)) return false;

                    float dSq = (c - enemyTarget.Position).LengthHorizontalSquared;

                    if (dSq <= currentDistSq) return false;
                    return dSq >= minSq && dSq <= maxSq;
                });

            return retreatCell.IsValid && retreatCell != pawn.Position;
        }

        protected override Job MeleeAttackJob(Pawn pawn, Thing target)
        {
            if (target == null || target.Destroyed) return null;
            return JobMaker.MakeJob(RimWorld.JobDefOf.AttackMelee, target);
        }

        protected bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verb, float rangeOverride)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                dest = IntVec3.Invalid;
                return false;
            }

            CastPositionRequest req = new CastPositionRequest
            {
                caster = pawn,
                target = enemyTarget,
                verb = verb,
                maxRangeFromTarget = rangeOverride,
                wantCoverFromTarget = false
            };

            return CastPositionFinder.TryFindCastPosition(req, out dest);
        }

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verb = null)
        {
            Verb v = verb;
            float r;
            if (v == null)
                v = GetLongestRangedVerb(pawn, out r);
            else
                r = v.verbProps?.range ?? 0f;

            if (v == null || r <= 0.1f)
            {
                dest = IntVec3.Invalid;
                return false;
            }

            return TryFindShootingPosition(pawn, out dest, v, r);
        }
    }
}