using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class CompProperties_AbilityPlaySound : CompProperties_AbilityEffect
    {
        public SoundDef soundDef;

        public CompProperties_AbilityPlaySound()
        {
            compClass = typeof(CompAbilityEffect_PlaySound);
        }
    }
    public class CompAbilityEffect_PlaySound : CompAbilityEffect
    {
        public CompProperties_AbilityPlaySound Props =>
            (CompProperties_AbilityPlaySound)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (Props.soundDef == null)
                return;

            Pawn caster = parent.pawn;

            if (caster?.Map == null)
                return;

            Props.soundDef.PlayOneShot(
                new TargetInfo(caster.Position, caster.Map));
        }
    }
}