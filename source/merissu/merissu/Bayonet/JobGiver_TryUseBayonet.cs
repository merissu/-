using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace merissu
{
	public class JobGiver_TryUseBayonet : ThinkNode_JobGiver
	{
		protected Job BayonetJob(Pawn attacker)
		{
			if (attacker == null || attacker.Dead || !attacker.Spawned || attacker.equipment == null)
			{
				return null;
			}
			ThingWithComps thing = attacker.equipment.Primary;
			if (thing == null || !(thing.TryGetComp<CompBayonet>() is CompBayonet comp) || !comp.AICanAttack)
			{
				return null;
			}

			System.Random random = new System.Random();
			int i = random.Next(0, 99);
			if (i >= 35)
            {
				return null;
            }
			comp.ticks = comp.Props.cooldown;
			if (attacker.jobs.curJob != null && attacker.CurJobDef != DefOf.MR_BayonetAttack)
			{
				attacker.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
			string str = comp.list.RandomElement();
			int j = random.Next(5, 10);
			//MoteMaker.ThrowText(attacker.PositionHeld.ToVector3() + new Vector3(0.5f, 0, 0.5f), attacker.MapHeld, str, j);
			Job job = JobMaker.MakeJob(DefOf.MR_BayonetAttack, attacker.mindState.enemyTarget);

			Hediff hediff = attacker.health.hediffSet.GetFirstHediffOfDef(DefOf.MR_BayonetCharge, false);
			if (hediff == null)
			{
				hediff = attacker.health.AddHediff(DefOf.MR_BayonetCharge, attacker.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = 1f;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = 300;
			}
			HediffComp_Bayonet hediffComp_Bayonet = hediff.TryGetComp<HediffComp_Bayonet>();
			if (hediffComp_Bayonet != null)
            {
				hediffComp_Bayonet.str = str;
            }
			return job;
		}
		protected override Job TryGiveJob(Pawn pawn)
		{
			return BayonetJob(pawn);
		}
	}
}
