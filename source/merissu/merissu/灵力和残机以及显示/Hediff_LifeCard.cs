using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace meriss
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

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("meriss.rimworld.lifecards");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_PreApplyDamage
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !pawn.Spawned) return true;

            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("InvincibleTime")))
            {
                return true;
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
}