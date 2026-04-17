using System;
using System.Collections.Generic;
using System.Linq; 
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobGiver_YinYangFirebeam : JobGiver_AIFightEnemy
    {
        public float maxAttackRange = 25f;
        public float minRetreatDist = 3f;
        public float maxRetreatDist = 8f;

        private const int TargetUpdateInterval = 15;
        private const int RetreatSearchInterval = 30;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null || enemyTarget.Destroyed || pawn.IsHashIntervalTick(TargetUpdateInterval))
            {
                UpdateEnemyTarget(pawn);
                enemyTarget = pawn.mindState.enemyTarget;
            }

            if (enemyTarget == null || enemyTarget.Destroyed) return null;

            Verb rangedVerb = GetRangedVerbNoLinq(pawn);
            if (rangedVerb == null) return null;

            float distSq = (pawn.Position - enemyTarget.Position).LengthHorizontalSquared;
            float minRetreatSq = minRetreatDist * minRetreatDist;
            float maxRetreatSq = maxRetreatDist * maxRetreatDist;
            float maxAttackSq = maxAttackRange * maxAttackRange;

            if (distSq > minRetreatSq && distSq < maxRetreatSq && pawn.IsHashIntervalTick(RetreatSearchInterval))
            {
                IntVec3 retreatCell = CellFinder.RandomClosewalkCellNear(
                    pawn.Position, pawn.Map, 5,
                    c => (c - enemyTarget.Position).LengthHorizontalSquared > distSq);

                if (retreatCell.IsValid && retreatCell != pawn.Position)
                {
                    Job j = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, retreatCell);
                    j.expiryInterval = 90;
                    j.checkOverrideOnExpire = true;
                    return j;
                }
            }

            if (distSq <= maxAttackSq && rangedVerb.CanHitTarget(enemyTarget))
            {
                Job shoot = JobMaker.MakeJob(RimWorld.JobDefOf.UseVerbOnThing, enemyTarget);
                shoot.verbToUse = rangedVerb;
                return shoot;
            }

            if (distSq > maxAttackSq && TryFindShootingPosition(pawn, out IntVec3 dest, rangedVerb))
            {
                Job j = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, dest);
                j.expiryInterval = 120;
                j.checkOverrideOnExpire = true;
                return j;
            }

            return JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, 30, true);
        }

        private Verb GetRangedVerbNoLinq(Pawn pawn)
        {
            var verbs = pawn.verbTracker?.AllVerbs;
            if (verbs == null) return null;
            for (int i = 0; i < verbs.Count; i++)
            {
                Verb v = verbs[i];
                if (!v.IsMeleeAttack && v.Available()) return v;
            }
            return null;
        }

        protected override Job MeleeAttackJob(Pawn pawn, Thing target) => null;

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verb = null)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null) { dest = IntVec3.Invalid; return false; }

            CastPositionRequest req = new CastPositionRequest
            {
                caster = pawn,
                target = enemyTarget,
                verb = verb ?? GetRangedVerbNoLinq(pawn),
                maxRangeFromTarget = maxAttackRange,
                wantCoverFromTarget = false
            };
            return CastPositionFinder.TryFindCastPosition(req, out dest);
        }
    }
}