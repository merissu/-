using Verse;
using RimWorld;

namespace merissu
{
    public class HediffComp_ScorchingBurn : HediffComp
    {
        public HediffCompProperties_ScorchingBurn Props => (HediffCompProperties_ScorchingBurn)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(60))
            {
                if (!Pawn.Spawned || Pawn.Dead) return;

                HediffDef buildUpDef = HediffDef.Named("scorchingbuildup_Hediff");
                Hediff buildup = Pawn.health.hediffSet.GetFirstHediffOfDef(buildUpDef);

                if (!Pawn.Position.Roofed(Pawn.Map))
                {
                    if (buildup == null)
                    {
                        buildup = HediffMaker.MakeHediff(buildUpDef, Pawn);
                        buildup.Severity = 0.05f;
                        Pawn.health.AddHediff(buildup);
                    }
                    else
                    {
                        buildup.Severity += 0.05f;
                    }
                }
                else
                {
                    if (buildup != null)
                    {
                        Pawn.health.RemoveHediff(buildup);
                    }
                }
            }
        }
    }

    public class HediffCompProperties_ScorchingBurn : HediffCompProperties
    {
        public HediffCompProperties_ScorchingBurn()
        {
            this.compClass = typeof(HediffComp_ScorchingBurn);
        }
    }


    public class HediffComp_ScorchingBuildUp : HediffComp
    {
        public HediffCompProperties_ScorchingBuildUp Props => (HediffCompProperties_ScorchingBuildUp)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Dead) return;

            if (Pawn.IsHashIntervalTick(60))
            {
                if (Pawn.Position.Roofed(Pawn.Map))
                {
                    Pawn.health.RemoveHediff(this.parent);
                    return;
                }

                int damageAmount = GenMath.RoundRandom(this.parent.Severity * 2f);
                if (damageAmount > 0)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, damageAmount);
                    Pawn.TakeDamage(dinfo);
                }

                if (this.parent.Severity >= 0.8f)
                {
                    FireUtility.TryAttachFire(Pawn, 0.5f, null);
                }
            }
        }
    }

    public class HediffCompProperties_ScorchingBuildUp : HediffCompProperties
    {
        public HediffCompProperties_ScorchingBuildUp()
        {
            this.compClass = typeof(HediffComp_ScorchingBuildUp);
        }
    }
}