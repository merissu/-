using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_StElmoPillar : Ability
    {
        public Ability_StElmoPillar() : base() { }

        public Ability_StElmoPillar(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足"; 
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f)
                return false;

            hp.Severity -= 1f;

            Projectile proj = (Projectile)GenSpawn.Spawn(
                ThingDef.Named("Projectile_StElmoFireball"),
                pawn.Position,
                pawn.Map);

            proj.Launch(
                pawn,
                pawn.DrawPos,
                target,
                target,
                ProjectileHitFlags.IntendedTarget);

            return true;
        }
    }
}