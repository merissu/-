using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_XWave : Ability
    {
        private static readonly HediffDef FullPowerDef = HediffDef.Named("FullPower");
        public Ability_XWave() : base() { }

        public Ability_XWave(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                if (fp == null || fp.Severity < 1f)
                {
                    return "灵力不足（需要1层）";
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

            if (fp != null && fp.Severity >= 1f)
            {
                fp.Severity -= 1f;

                SoundDef soundDef = SoundDef.Named("XWave");
                if (soundDef != null)
                {
                    soundDef.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }

                Thing thing = ThingMaker.MakeThing(ThingDef.Named("XWaveEffect"));
                if (thing is Thing_XWaveEffect effect)
                {
                    effect.caster = pawn;
                    GenSpawn.Spawn(effect, pawn.Position, pawn.Map);
                }

                return true;
            }

            return false;
        }
    }
}