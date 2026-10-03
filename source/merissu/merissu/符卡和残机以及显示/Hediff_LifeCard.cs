using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Hediff_LifeCard : HediffWithComps
    {
        public void UseOneLife()
        {
            SoundDef.Named("biu").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            this.Severity -= 1f;

            ThingDef powerPointDef = ThingDef.Named("PowerPoint");
            if (powerPointDef != null)
            {
                Thing thing = ThingMaker.MakeThing(powerPointDef);
                thing.stackCount = 10;
                GenSpawn.Spawn(thing, pawn.Position, pawn.Map);
            }

            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref injuries);
            foreach (var injury in injuries)
            {
                pawn.health.RemoveHediff(injury);
            }

            Hediff invincible = HediffMaker.MakeHediff(HediffDef.Named("InvincibleTime"), pawn);
            invincible.Severity = 1f;
            pawn.health.AddHediff(invincible);

        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_PreApplyDamage
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            Pawn pawn = __instance.pawn;  
            if (pawn == null || !pawn.Spawned) return true;

            if (pawn.def.defName == "ZayuLily" && dinfo.Def.harmsHealth && dinfo.Instigator != null && dinfo.Instigator != pawn)
            {
                LilyRageHelper.TryEnrage(pawn);
            }

            CompLilyDanmakuTracker tracker = pawn.TryGetComp<CompLilyDanmakuTracker>();
            if (pawn.def.defName == "ZayuLily" && dinfo.Def.harmsHealth && tracker != null && !tracker.livesGranted)
            {
                tracker.livesGranted = true;
                Hediff lifeCard = HediffMaker.MakeHediff(HediffDef.Named("up"), pawn);
                lifeCard.Severity = 2f;
                pawn.health.AddHediff(lifeCard);
                return true;
            }

            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("InvincibleTime")))
            {
                return true;
            }

            Hediff durga = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("DurgaSoul"));
            if (durga != null)
            {
                HediffComp_DurgaSoul comp = durga.TryGetComp<HediffComp_DurgaSoul>();
                if (comp != null && comp.remainingCharges > 0)
                {
                    return true; 
                }
            }

            var lifeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("up")) as Hediff_LifeCard;
            if (lifeHediff != null && lifeHediff.Severity >= 1f && dinfo.Def.harmsHealth)
            {
                lifeHediff.UseOneLife();
                absorbed = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
    public static class Patch_Verb_TryCastShot_LilyRage
    {
        public static void Postfix(Verb_Shoot __instance)
        {
            LilyRageHelper.TryEnrage(__instance.currentTarget.Pawn);
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_Verb_MeleeAttack_TryCastShot_LilyRage
    {
        public static void Postfix(Verb_MeleeAttack __instance)
        {
            LilyRageHelper.TryEnrage(__instance.currentTarget.Pawn);
        }
    }

    public static class LilyRageHelper
    {
        public static void TryEnrage(Pawn pawn)
        {
            if (pawn == null || pawn.def.defName != "ZayuLily" || pawn.Dead) return;

            if (pawn.mindState != null && pawn.mindState.mentalStateHandler != null && !pawn.mindState.mentalStateHandler.InMentalState)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true);
            }
        }
    }
}