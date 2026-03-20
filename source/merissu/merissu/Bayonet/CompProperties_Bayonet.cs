using System;
using Verse;
using RimWorld;

namespace merissu
{
	public class CompProperties_Bayonet : CompProperties
	{
		public CompProperties_Bayonet()
		{
			this.compClass = typeof(CompBayonet);
		}

		public int cooldown = 1800;

		public float radius;

		public string label;

		public string description;

		public string icon;

		public int damageMultiplier = 1;
	}
}
