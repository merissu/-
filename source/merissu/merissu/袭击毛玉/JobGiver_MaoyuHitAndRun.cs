using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace merissu
{
    public class JobGiver_MaoyuHitAndRun : JobGiver_AIFightEnemy
    {
        public float minEscapeDist = 6f;
        public float maxEscapeDist = 15f;
        public float personalSpaceSq = 9f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            UpdateEnemyTarget(pawn);
            Thing target = pawn.mindState.enemyTarget;

            if (target == null || target.Destroyed) return null;

            Ability activeAbility = null;
            if (pawn.abilities != null)
            {
                var abilities = pawn.abilities.AllAbilitiesForReading;

                for (int i = 0; i < abilities.Count; i++)
                {
                    if (abilities[i].CanCast)
                    {
                        activeAbility = abilities[i];
                        break;
                    }
                }
            }
            if (activeAbility != null)
            {
                float distSq = pawn.Position.DistanceToSquared(target.Position);
                float range = activeAbility.verb.verbProps.range;

                if (distSq <= range * range)
                {
                    return activeAbility.GetJob(target, target);
                }

                return JobMaker.MakeJob(RimWorld.JobDefOf.Goto, target.Position);
            }

            if (TryFindEscapeCell(pawn, target, out IntVec3 escapeCell))
            {
                Job retreat = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, escapeCell);
                retreat.expiryInterval = 120;
                return retreat;
            }

            return base.TryGiveJob(pawn);
        }

        private bool TryFindEscapeCell(Pawn pawn, Thing enemy, out IntVec3 cell)
        {
            Map map = pawn.Map;
            IntVec3 enemyPos = enemy.Position;
            Verb currentVerb = pawn.CurrentEffectiveVerb; 

            CastPositionRequest request = new CastPositionRequest();
            request.caster = pawn;
            request.target = enemy;
            request.verb = currentVerb;
            request.maxRangeFromTarget = maxEscapeDist;
            request.wantCoverFromTarget = true;
            request.maxRegions = 15; 

            request.validator = (IntVec3 c) =>
            {
                float distSq = c.DistanceToSquared(enemyPos);
                if (distSq < 36f || distSq > 225f) return false; 

                if (PawnUtility.AnyPawnBlockingPathAt(c, pawn)) return false;

                return true;
            };

            if (CastPositionFinder.TryFindCastPosition(request, out cell))
            {
                return true;
            }

            return CellFinder.TryFindRandomReachableNearbyCell(
                pawn.Position,
                map,
                maxEscapeDist,
                TraverseParms.For(pawn),
                c => {
                    float distSq = c.DistanceToSquared(enemyPos);
                    return distSq >= 36f && distSq <= 225f &&
                           distSq > pawn.Position.DistanceToSquared(enemyPos) &&
                           c.Standable(map) &&
                           !PawnUtility.AnyPawnBlockingPathAt(c, pawn) &&
                           GenSight.LineOfSight(c, enemyPos, map);
                },
                null,
                out cell,
                30 
            );
        }

        private bool IsSpaceOccupied(IntVec3 c, Pawn pawn, Map map)
        {
            return PawnUtility.AnyPawnBlockingPathAt(c, pawn);
        }

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse)
        {
            dest = IntVec3.Invalid;
            return false;
        }
    }
}