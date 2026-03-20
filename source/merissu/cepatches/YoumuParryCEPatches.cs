using System;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class YoumuCEStartup
    {
        static YoumuCEStartup()
        {
            var harmony = new Harmony("merissu.youmu.ce.patch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(BulletCE), "Impact", new Type[] { typeof(Thing) })]
    public static class BulletCE_YoumuParry_Patch
    {
        public static bool Prefix(Thing hitThing, BulletCE __instance, ref Thing ___launcher, ref Thing ___intendedTarget, ref Ray ___shotLine, ref float ___shotRotation, ref Vector2 ___origin, ref bool ___landed)
        {
            if (!(hitThing is Pawn pawn) || pawn.Map == null) return true;

            var parryHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("Youmuparry"));
            if (parryHediff == null || pawn.equipment?.Primary?.def?.defName != "BailouSword") return true;

            GenClamor.DoClamor(pawn, 2.1f, ClamorDefOf.Impact);
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ShotFlash);
            SoundDefOf.MetalHitImportant.PlayOneShot(pawn);
            MoteMaker.ThrowText(pawn.Position.ToVector3(), pawn.Map, "反射下界斩!");
            GenSpawn.Spawn(ThingDef.Named("YoumuParryAnim"), pawn.Position, pawn.Map);

            pawn.Drawer.Notify_DamageDeflected(new DamageInfo(__instance.def.projectile.damageDef, 1f));
            pawn.skills?.Learn(SkillDefOf.Melee, 200f);

            if (___launcher != null)
            {
                ___intendedTarget = ___launcher;

                Vector2 oldOrigin = ___origin;

                ___origin = new Vector2(__instance.Position.x, __instance.Position.z);

                __instance.Destination = oldOrigin;

                ___shotRotation = (___shotRotation + 180f) % 360f;

                ___shotLine = new Ray(___shotLine.origin, -___shotLine.direction);

                ___launcher = pawn;

                ___landed = false;

            }
            else
            {
                __instance.Destroy(DestroyMode.Vanish);
            }

            return false; 
        }
    }
}