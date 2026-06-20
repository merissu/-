using System.Collections.Generic;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_Vampire : HediffCompProperties
    {
        public HediffCompProperties_Vampire()
        {
            compClass = typeof(HediffComp_Vampire);
        }
    }

    public class HediffComp_Vampire : HediffComp
    {
        private new Pawn Pawn => parent.pawn;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            if (Pawn == null) return;

            List<Hediff> removeList = new List<Hediff>();

            foreach (var h in Pawn.health.hediffSet.hediffs)
            {
                if (h.def.isBad && !(h is Hediff_Injury))
                {
                    removeList.Add(h);
                }
            }

            foreach (var h in removeList)
            {
                Pawn.health.RemoveHediff(h);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Dead) return;

            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            Pawn.health.hediffSet.GetHediffs(ref injuries);

            int count = 0;

            foreach (var inj in injuries)
            {
                if (!inj.IsPermanent())
                    count++;
            }

            if (count == 0) return;

            float multiplier = 1f + count;

            foreach (var inj in injuries)
            {
                if (!inj.IsPermanent())
                {
                    inj.Heal(0.01f * multiplier);
                }
            }
        }
    }
}