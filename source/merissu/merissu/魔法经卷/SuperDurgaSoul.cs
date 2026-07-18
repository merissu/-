using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_superDurgaSoul : Ability
    {
        public Ability_superDurgaSoul() : base() { }
        public Ability_superDurgaSoul(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.Activate(target, dest)) return false;
            if (pawn == null) return false;

            HediffDef superDurgaDef = HediffDef.Named("superDurgaSoul");
            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(superDurgaDef);

            if (existingHediff != null)
            {
                HediffComp_Disappears compDisappears = existingHediff.TryGetComp<HediffComp_Disappears>();
                if (compDisappears != null)
                {
                    compDisappears.ticksToDisappear = 3600;
                }
            }
            else
            {
                pawn.health.AddHediff(superDurgaDef);
            }

            return true;
        }
    }
    public class HediffCompProperties_SuperDurgaSoul : HediffCompProperties
    {
        public int invincibleTimeTicks = 10;

        public HediffCompProperties_SuperDurgaSoul()
        {
            compClass = typeof(HediffComp_SuperDurgaSoul);
        }
    }
    public class HediffComp_SuperDurgaSoul : HediffComp
    {
        public int invincibleTicksLeft;
        private int auraTickCounter;
        private bool toggle;

        public HediffCompProperties_SuperDurgaSoul Props => (HediffCompProperties_SuperDurgaSoul)props;

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

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref invincibleTicksLeft, "invincibleTicksLeft", 0);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    public static class Patch_SuperDurgaSoulAbsorb
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            Pawn pawn = __instance.pawn;
            if (pawn == null) return true;

            Hediff superDurga = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("superDurgaSoul"));
            if (superDurga == null) return true;

            HediffComp_SuperDurgaSoul comp = superDurga.TryGetComp<HediffComp_SuperDurgaSoul>();
            if (comp != null)
            {
                absorbed = true;
                comp.invincibleTicksLeft = comp.Props.invincibleTimeTicks;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Projectile), "ImpactSomething")]
    public static class Patch_SuperDurgaSoulParry
    {
        [HarmonyPrefix]
        public static bool Prefix(Projectile __instance, LocalTargetInfo ___usedTarget, Thing ___launcher)
        {
            if (!(___usedTarget.Thing is Pawn pawn))
                return true;

            if (pawn.health?.hediffSet == null ||
                pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("superDurgaSoul")) == null)
                return true;

            if (___launcher == null)
                return true;

            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ShotFlash);
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            pawn.Drawer.Notify_DamageDeflected(new DamageInfo(__instance.def.projectile.damageDef, 0f));

            ThingDef projectileDef = __instance.def;
            __instance.Destroy(DestroyMode.Vanish);

            Projectile rebound = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);

            rebound.Launch(
                launcher: pawn,
                origin: pawn.Position.ToVector3Shifted(),
                usedTarget: new LocalTargetInfo(___launcher),
                intendedTarget: new LocalTargetInfo(___launcher),
                hitFlags: ProjectileHitFlags.All,
                preventFriendlyFire: false,
                equipment: pawn.equipment?.Primary
            );

            return false;
        }
    }
}