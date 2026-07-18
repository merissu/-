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
    public static class DurgaSoulCEStartup
    {
        static DurgaSoulCEStartup()
        {
            var harmony = new Harmony("merissu.durgasoul.ce.patch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(BulletCE), "Impact", new Type[] { typeof(Thing) })]
    public static class BulletCE_DurgaSoulParry_Patch
    {
        public static bool Prefix(
            Thing hitThing,
            BulletCE __instance,
            ref Thing ___launcher,
            ref Thing ___intendedTarget,
            ref Ray ___shotLine,
            ref float ___shotRotation,
            ref Vector2 ___origin,
            ref bool ___landed)
        {
            if (!(hitThing is Pawn pawn) || pawn.Map == null)
                return true;

            if (pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("superDurgaSoul")) == null)
                return true;

            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ShotFlash);
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            pawn.Drawer.Notify_DamageDeflected(new DamageInfo(__instance.def.projectile.damageDef, 0f));

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