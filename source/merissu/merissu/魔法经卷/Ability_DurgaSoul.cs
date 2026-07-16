using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Ability_DurgaSoul : Ability
    {
        public Ability_DurgaSoul() : base() { }
        public Ability_DurgaSoul(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.Activate(target, dest)) return false;
            if (pawn == null) return false;

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ShinkiBuff_Defense");
            if (buffDef != null)
            {
                Hediff defenseBuff = pawn.health.hediffSet.GetFirstHediffOfDef(buffDef);
                if (defenseBuff != null)
                {
                    HediffDef hijiriDef = DefDatabase<HediffDef>.GetNamedSilentFail("hijiriShinkiRecitation");
                    if (hijiriDef == null || pawn.health.hediffSet.GetFirstHediffOfDef(hijiriDef) == null)
                    {
                        pawn.health.RemoveHediff(defenseBuff);
                    }
                }
            }

            Hediff durga = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("DurgaSoul"));
            if (durga != null)
            {
                HediffComp_DurgaSoul comp = durga.TryGetComp<HediffComp_DurgaSoul>();
                if (comp != null)
                    comp.ResetCharges();
            }
            else
            {
                pawn.health.AddHediff(HediffDef.Named("DurgaSoul"));
            }

            return true;
        }
    }

    public class HediffCompProperties_DurgaSoul : HediffCompProperties
    {
        public int maxCharges = 5;
        public int invincibleTimeTicks = 60; 

        public HediffCompProperties_DurgaSoul()
        {
            compClass = typeof(HediffComp_DurgaSoul);
        }
    }

    public class HediffComp_DurgaSoul : HediffComp
    {
        public int remainingCharges;
        public int invincibleTicksLeft;
        private bool initialized;

        private int auraTickCounter;
        private bool toggle;

        public HediffCompProperties_DurgaSoul Props => (HediffCompProperties_DurgaSoul)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            if (!initialized)
            {
                remainingCharges = Props.maxCharges;
                initialized = true;
            }
        }

        public void ResetCharges()
        {
            remainingCharges = Props.maxCharges;
            invincibleTicksLeft = 0; 
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!Pawn.Spawned || Pawn.Dead) return;

            if (invincibleTicksLeft > 0)
                invincibleTicksLeft--;

            auraTickCounter++;
            if (auraTickCounter >= 30) 
            {
                auraTickCounter = 0;
                SpawnAura();
            }
        }

        private void SpawnAura()
        {
            ThingDef auraDef = toggle ? ThingDef.Named("Mote_DurgaAuraA") : ThingDef.Named("Mote_DurgaAuraB");
            toggle = !toggle;
            if (auraDef != null)
            {
                MoteMaker.MakeAttachedOverlay(Pawn, auraDef, new Vector3(0f, 0f, -0.7f));
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
        public static class Patch_DurgaSoulAbsorb
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool absorbed)
            {
                absorbed = false;
                Pawn pawn = __instance.pawn;
                if (pawn == null) return true;

                Hediff durga = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("DurgaSoul"));
                if (durga == null) return true;

                HediffComp_DurgaSoul comp = durga.TryGetComp<HediffComp_DurgaSoul>();
                if (comp != null && comp.remainingCharges > 0)
                {
                    if (comp.invincibleTicksLeft > 0)
                    {
                        absorbed = true;
                        return false;
                    }

                    comp.remainingCharges--;
                    absorbed = true;
                    comp.invincibleTicksLeft = comp.Props.invincibleTimeTicks;

                    if (comp.remainingCharges <= 0)
                        pawn.health.RemoveHediff(durga);

                    return false;
                }
                return true;
            }
        }

        public override string CompLabelInBracketsExtra => $"{remainingCharges}";

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref remainingCharges, "remainingCharges", Props.maxCharges);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref invincibleTicksLeft, "invincibleTicksLeft", 0);
        }
    }
}