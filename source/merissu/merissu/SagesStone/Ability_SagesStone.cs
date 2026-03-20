using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_SagesStone : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");

        private const float PowerCost = 5.0f;
        public Ability_SagesStone() : base() { }

        public Ability_SagesStone(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < PowerCost)
                {
                    return "灵力不足（需要5层）";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < PowerCost)
                return false;

            fp.Severity -= PowerCost;
            if (fp.Severity <= 0f)
            {
                pawn.health.RemoveHediff(fp);
            }

            Thing controller = ThingMaker.MakeThing(
                ThingDef.Named("SagesStoneOrbitController"));

            GenSpawn.Spawn(controller, pawn.Position, pawn.Map);

            Thing_SagesStoneOrbitController c =
                controller as Thing_SagesStoneOrbitController;

            if (c != null)
            {
                c.Init(pawn);
            }

            HediffDef sageHediffDef = HediffDef.Named("SagesStone");

            if (sageHediffDef != null)
            {
                Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(sageHediffDef);

                if (existingHediff != null)
                {
                    pawn.health.RemoveHediff(existingHediff);
                }

                pawn.health.AddHediff(sageHediffDef);
            }

            return base.Activate(target, dest);
        }
    }
}