using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Comp_HakureiShield : CompShield
    {
        private bool lastTickShieldUp = true;

        public new CompProperties_HakureiShield Props =>
            (CompProperties_HakureiShield)props;

        public override void CompTick()
        {
            base.CompTick();

            bool shieldUpNow = ShieldUp();

            if (lastTickShieldUp && !shieldUpNow)
            {
                OnShieldBroken();
            }

            lastTickShieldUp = shieldUpNow;
        }

        private bool ShieldUp()
        {
            return Energy > 0f && ticksToReset <= 0;
        }

        private void OnShieldBroken()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null) return;

            SoundDef.Named("HakureiAmulet")
                .PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            if (Props.fullPowerHediff == null) return;

            Hediff hediff =
                pawn.health.hediffSet.GetFirstHediffOfDef(
                    Props.fullPowerHediff);

            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(
                    Props.fullPowerHediff);
                hediff.Severity = 1f;
            }
            else
            {
                hediff.Severity =
                    Mathf.Min(
                        hediff.Severity + 1f,
                        Props.maxSeverity);
            }
        }
    }
}
