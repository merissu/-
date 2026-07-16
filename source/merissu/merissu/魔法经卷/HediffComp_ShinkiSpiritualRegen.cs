using Verse;

namespace merissu
{
    public class HediffCompProperties_ShinkiSpiritualRegen : HediffCompProperties
    {
        public HediffCompProperties_ShinkiSpiritualRegen()
        {
            compClass = typeof(HediffComp_ShinkiSpiritualRegen);
        }
    }

    public class HediffComp_ShinkiSpiritualRegen : HediffComp
    {
        private const float SeverityGainPerTick = 0.0005f; 

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn?.health == null) return;

            HediffDef spDef = HediffDef.Named("spiritualpower");
            if (spDef == null) return;

            Hediff spHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(spDef);
            if (spHediff == null)
            {
                spHediff = HediffMaker.MakeHediff(spDef, Pawn);
                spHediff.Severity = 0f;
                Pawn.health.AddHediff(spHediff);
            }

            if (spHediff.Severity < 1f)
            {
                spHediff.Severity += SeverityGainPerTick;
                if (spHediff.Severity > 1f)
                    spHediff.Severity = 1f;
            }
        }
    }
}