using System;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace merissu
{
	public class CompGetGizmo : ThingComp
	{
		public CompProperties_GetGizmo Props
		{
			get
			{
				return this.props as CompProperties_GetGizmo;
			}
		}

        public override void CompTick()
        {
            base.CompTick();
			ticks++;
			if (ticks >= 60)
            {
				ticks = 0;
				Pawn p = this.parent as Pawn;
				if (p == null || p.Dead || !p.Spawned || p.equipment == null)
				{
					return;
				}
				ThingWithComps thing = p.equipment.Primary;
				if (thing == null)
				{
					return;
				}
				CompBayonet comp = thing.TryGetComp<CompBayonet>();
				if (comp != null)
				{
					comp.ticks -= 60;
				}
			}
		}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			Pawn p = this.parent as Pawn;
			if (p == null || p.Dead || !p.Spawned || p.equipment == null || !p.Drafted)
			{
				yield break;
			}
			ThingWithComps thing = p.equipment.Primary;
			if (thing == null)
			{
				yield break;
			}
			CompBayonet comp = thing.TryGetComp<CompBayonet>();
			if (comp != null)
			{
				foreach (Gizmo g in comp.CompGetGizmosExtra())
				{
					yield return g;
				}
			}
			yield break;
		}

		private int ticks;
	}
}
