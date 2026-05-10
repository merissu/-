using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Hediff_Gypsum : Hediff_High
    {
        public override void PostTick()
        {
            base.PostTick();

            if (pawn.IsHashIntervalTick(30))
            {
                MaintainDeathRefusal();
            }
        }

        private void MaintainDeathRefusal()
        {
            Hediff deathRefusal = pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.DeathRefusal);
            if (deathRefusal == null)
            {
                deathRefusal = pawn.health.AddHediff(RimWorld.HediffDefOf.DeathRefusal);
            }

            if (deathRefusal.Severity < deathRefusal.def.maxSeverity)
            {
                deathRefusal.Severity = deathRefusal.def.maxSeverity;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_InstantDeath
    {
        public static bool Prefix(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (!___pawn.Dead && ___pawn.health.hediffSet.HasHediff(HediffDef.Named("Gypsum")))
            {
                if (dinfo.Def != null && dinfo.Def.ExternalViolenceFor(___pawn))
                {
                    ___pawn.Kill(dinfo);

                    SoundDef biuSound = SoundDef.Named("biu");
                    if (biuSound != null)
                    {
                        biuSound.PlayOneShot(new TargetInfo(___pawn.Position, ___pawn.Map));
                    }

                    Hediff sickness = ___pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.DeathRefusalSickness);
                    if (sickness != null)
                    {
                        ___pawn.health.RemoveHediff(sickness);
                    }

                    absorbed = true;
                    return false;
                }
            }
            return true;
        }
    }
}