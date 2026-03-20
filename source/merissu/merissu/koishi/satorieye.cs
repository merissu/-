using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompsProperties_MindReading : HediffCompProperties
    {
        public HediffCompsProperties_MindReading()
        {
            this.compClass = typeof(HediffComps_MindReading);
        }
    }

    public class HediffComps_MindReading : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
        }
    }
    public class TerribleHypnosis : Ability
    {
        public TerribleHypnosis() : base() { }

        public TerribleHypnosis(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {

            HediffDef hediffToAdd = HediffDef.Named("TerribleHypnosis");
            if (hediffToAdd != null)
            {
                HealthUtility.AdjustSeverity(pawn, hediffToAdd, 1f);
            }

            return base.Activate(target, dest);
        }
    }

    public class ThoughtWorker_SatoriEye : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (pawn.health.hediffSet.HasHediff(merissu.HediffDefOf.KoishisEye))
            {
                return false;
            }

            if (other.health.hediffSet.HasHediff(merissu.HediffDefOf.SatorisEye))
            {
                return true;
            }

            return false;
        }
    }
}