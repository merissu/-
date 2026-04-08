using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace merissu
{
    public class HediffComp_FrogAura : HediffComp
    {
        public HediffCompProperties_FrogAura Props => (HediffCompProperties_FrogAura)props;

        private int tickCounter = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            tickCounter++;
            if (tickCounter >= 120)
            {
                tickCounter = 0;
                ApplyEffect();
            }
        }

        private void ApplyEffect()
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            float range = 40f;
            Map map = pawn.Map;

            IEnumerable<Pawn> targets = GenRadial.RadialDistinctThingsAround(pawn.Position, map, range, true).OfType<Pawn>();

            foreach (Pawn target in targets)
            {
                if (target.HostileTo(pawn) && !target.Dead)
                {
                    HediffDef frogBoomDef = HediffDef.Named("FrogBoom");
                    if (target.health.hediffSet.GetFirstHediffOfDef(frogBoomDef) == null)
                    {
                        target.health.AddHediff(frogBoomDef);
                    }
                }
            }
        }
    }

    public class HediffCompProperties_FrogAura : HediffCompProperties
    {
        public HediffCompProperties_FrogAura()
        {
            compClass = typeof(HediffComp_FrogAura);
        }
    }
}