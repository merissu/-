using System;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace merissu
{
	public class JobDriver_Bayonet : JobDriver_Goto
	{
		protected override IEnumerable<Toil> MakeNewToils()
		{
			LocalTargetInfo lookAtTarget = job.GetTarget(TargetIndex.B);
			Toil toil = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			toil.AddPreTickAction(delegate
			{
				if (job.exitMapOnArrival && pawn.Map.exitMapGrid.IsExitCell(pawn.Position))
				{
					TryExitMap();
				}
			});
			toil.AddPreTickAction(delegate
			{
				Effecter effecter = DefOf.Berserk.SpawnAttached(pawn, pawn.MapHeld, 1f);
				effecter.Trigger(pawn, pawn, -1);
				effecter.Cleanup();
			});
			toil.FailOn(() => job.failIfCantJoinOrCreateCaravan && !RimWorld.Planet.CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(pawn));
			toil.FailOn(() => job.GetTarget(TargetIndex.A).Thing is Pawn p && p.ParentHolder is Corpse);
			toil.FailOn(() => job.GetTarget(TargetIndex.A).Thing?.Destroyed ?? false);
			toil.FailOn(() => pawn.equipment == null || !pawn.equipment.HasAnything() || pawn.equipment.Primary == null || pawn.equipment.Primary.TryGetComp<CompBayonet>() == null);
			if (lookAtTarget.IsValid)
			{
				toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
				{
					pawn.rotationTracker.FaceCell(lookAtTarget.Cell);
				});
				toil.handlingFacing = true;
			}
			toil.AddFinishAction(delegate
			{
				if (job.controlGroupTag != null && job.controlGroupTag != null)
				{
					pawn.GetOverseer()?.mechanitor.GetControlGroup(pawn).SetTag(pawn, job.controlGroupTag);
				}
			});
			toil.AddFinishAction(delegate
			{
				if (job.GetTarget(TargetIndex.A).Thing is Pawn p && pawn.CanReachImmediate(job.targetA, PathEndMode.Touch))
				{
					BayonetDamage(p);
				}
			});
			yield return toil;
			Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
			toil2.initAction = delegate
			{
				if (pawn.mindState != null && pawn.mindState.forcedGotoPosition == base.TargetA.Cell)
				{
					pawn.mindState.forcedGotoPosition = IntVec3.Invalid;
				}
				if (!job.ritualTag.NullOrEmpty() && LordUtility.GetLord(pawn)?.LordJob is LordJob_Ritual lordJob_Ritual)
				{
					lordJob_Ritual.AddTagForPawn(pawn, job.ritualTag);
				}
				if (job.exitMapOnArrival && (pawn.Position.OnEdge(pawn.Map) || pawn.Map.exitMapGrid.IsExitCell(pawn.Position)))
				{
					TryExitMap();
				}
			};
			toil2.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return toil2;
		}

		private void TryExitMap()
		{
			if (!job.failIfCantJoinOrCreateCaravan || RimWorld.Planet.CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(pawn))
			{
				if (ModsConfig.BiotechActive)
				{
					MechanitorUtility.Notify_PawnGotoLeftMap(pawn, pawn.Map);
				}
				pawn.ExitMap(allowedToJoinOrCreateCaravan: true, CellRect.WholeMap(base.Map).GetClosestEdge(pawn.Position));
			}
		}

		private void BayonetDamage(Pawn victim)
        {
			DefOf.MR_BayonetSound.PlayOneShot(new TargetInfo(victim.PositionHeld, victim.MapHeld, false));
			DamageVictim(victim);
			pawn.stances.stagger.StaggerFor(120);
		}

		public void DamageVictim(Pawn victim)
		{
			if (victim.Dead)
			{
				return;
			}
			HediffSet hediffSet = victim.health.hediffSet;
			BodyPartRecord heart = GetHeart(victim.health.hediffSet);
			IEnumerable<BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
												 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
												 select x;
			BodyPartRecord bodyPartRecord = heart ?? source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
			if (bodyPartRecord == null)
			{
				return;
			}
			int maxHitPoints = bodyPartRecord.def.hitPoints;
			int num = (int)(maxHitPoints / victim.GetStatValue(StatDefOf.IncomingDamageFactor)) * 4;
			float penetration;
			if (ModLister.HasActiveModWithName("Combat Extended"))
            {
				penetration = 5f;
            }
            else
            {
				penetration = 0.5f;
            }
			num *= pawn.equipment.Primary.TryGetComp<CompBayonet>().Props.damageMultiplier;
			penetration *= pawn.equipment.Primary.TryGetComp<CompBayonet>().Props.damageMultiplier;
			victim.TakeDamage(new DamageInfo(DamageDefOf.Cut, (float)num, penetration, 0f, pawn, bodyPartRecord, DefOf.MR_Bayonet, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
		}

		private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
		{
			return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
				   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
				   select x;
		}

		public BodyPartRecord GetHeart(HediffSet hediffSet)
		{
			foreach (BodyPartRecord notMissingPart in hediffSet.GetNotMissingParts())
			{
				if (notMissingPart.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource))
				{
					return notMissingPart;
				}
			}

			return null;
		}
	}
}
