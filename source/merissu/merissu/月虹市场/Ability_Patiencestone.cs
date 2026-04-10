using RimWorld;
using Verse;

namespace merissu
{
    public class Ability_Patiencestone : Ability
    {
        public Ability_Patiencestone() : base() { }

        public Ability_Patiencestone(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                return AcceptanceReport.WasAccepted;
            }
        }
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null)
                return false;

            Projectile proj = (Projectile)GenSpawn.Spawn(
                ThingDef.Named("UniverseQuake"),
                pawn.Position,
                pawn.Map);

            proj.Launch(
                pawn,
                pawn.DrawPos,
                target,
                target,
                ProjectileHitFlags.IntendedTarget);

            this.StartCooldown(this.def.cooldownTicksRange.RandomInRange);

            return true;
        }
    }
}