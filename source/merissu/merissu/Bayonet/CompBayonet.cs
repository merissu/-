using System;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
	[StaticConstructorOnStartup]
	public class CompBayonet : ThingComp
    {
		public CompProperties_Bayonet Props
		{
			get
			{
				return this.props as CompProperties_Bayonet;
			}
		}

		public Pawn pawn
        {
            get
            {
				if (!(base.ParentHolder is Pawn_EquipmentTracker pawn_EquipmentTracker))
				{
					return null;
				}
				return pawn_EquipmentTracker.pawn;
			}
        }

		public bool AICanAttack
		{
			get
			{
				if (pawn == null || pawn.Map == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.mindState.mentalStateHandler.InMentalState || pawn.Faction == null)
                {
					return false;
                }
				if (ticks > 0 || !(pawn.mindState.enemyTarget is Pawn p))
                {
					return false;
                }
				if (pawn.Map != p.Map || !p.Position.IsValid || pawn.Position.DistanceTo(p.Position) < 2.9f || pawn.Position.DistanceTo(p.Position) > this.Props.radius || !pawn.CanReach(p, PathEndMode.Touch, Danger.Deadly))
                {
					return false;
                }
				if (!pawn.CanSee(p))
                {
					return false;
                }
				return true;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.pawn != null && this.pawn.Faction != null && this.pawn.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action act = new Command_Action
				{
					hotKey = KeyBindingDefOf.Misc12,
					defaultLabel = this.Props.label.Translate(),
					defaultDesc = this.Props.description.Translate(),
					icon = ticks <= 0 ? ContentFinder<Texture2D>.Get(this.Props.icon, true) : ContentFinder<Texture2D>.Get(this.Props.icon + "_cooldown", true),
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectCorpseTargetParameters(), new Action<LocalTargetInfo>(this.BayonetAct), null, new Func<LocalTargetInfo, bool>(this.CanAffect), null, null, null, true, null);
					}
				};
				if (ticks > 0)
				{
					act.Disable("merissu_BayonetCooldown".Translate(Mathf.Floor(ticks / 60).ToString().Colorize(Color.cyan)));
				}
				if (pawn.Downed)
                {
					act.Disable("merissu_BayonetDowned".Translate());
				}
				yield return act;
			}
			if (DebugSettings.ShowDevGizmos)
			{
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "DEV: Reset cooldown";
				command_Action.action = delegate
				{
					ticks = 0;
				};
				yield return command_Action;
			}
			yield break;
		}

		public TargetingParameters ConnectCorpseTargetParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = true,
				canTargetBuildings = false,
				canTargetHumans = true,
				canTargetMechs = true,
				canTargetAnimals = true,
				canTargetLocations = false,
				validator = ((TargetInfo x) => this.CanAffect((LocalTargetInfo)x))
			};
		}

		public bool CanAffect(LocalTargetInfo target)
		{
			IntVec3 vec3 = target.Cell;
			GenDraw.DrawRadiusRing(pawn.Position, this.Props.radius, Color.red);
			GenDraw.DrawRadiusRing(pawn.Position, 2.9f, Color.red);
			return vec3.IsValid && target.Thing is Pawn && pawn.Position.DistanceTo(target.Cell) <= this.Props.radius && pawn.Position.DistanceTo(target.Cell) >= 2.9f && pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly);
		}

		public void BayonetAct(LocalTargetInfo target)
		{
			this.ticks = this.Props.cooldown;
			Pawn p = target.Pawn;
			System.Random random = new System.Random();
			string str = list.RandomElement();
			int i = random.Next(5, 10);
			//MoteMaker.ThrowText(pawn.PositionHeld.ToVector3() + new Vector3(0.5f, 0, 0.5f), pawn.MapHeld, str, i);
			Job job = JobMaker.MakeJob(DefOf.MR_BayonetAttack, p);
			pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder, requestQueueing: false);

			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefOf.MR_BayonetCharge, false);
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(DefOf.MR_BayonetCharge, pawn.health.hediffSet.GetBrain(), null, null);
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
		}

		public int ticks = 0;

		public List<string> list = new List<string>()
		{
			"MR.BayonetCry.A".Translate(),
			"MR.BayonetCry.B".Translate(),
			"MR.BayonetCry.C".Translate(),
			"MR.BayonetCry.D".Translate(),
			"MR.BayonetCry.E".Translate(),
		};
	}
}
