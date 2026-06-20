using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace merissu
{
    [HarmonyPatch(typeof(Projectile), "ImpactSomething")]
    public static class Projectile_YoumuParry
    {
        public static bool Prefix(
            Projectile __instance,
            LocalTargetInfo ___usedTarget,
            LocalTargetInfo ___intendedTarget,
            Thing ___launcher)
        {
            if (!(___usedTarget.Thing is Pawn pawn))
                return true;

            if (pawn.health?.hediffSet == null ||
                pawn.health.hediffSet.GetFirstHediffOfDef(
                    HediffDef.Named("Youmuparry")) == null)
                return true;

            if (pawn.equipment?.Primary == null ||
                pawn.equipment.Primary.def.defName != "BailouSword")
                return true;

            if (___launcher == null)
                return true;

            GenSpawn.Spawn(
                ThingDef.Named("YoumuParryAnim"),
                pawn.Position,
                pawn.Map);

            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.ShotFlash);
            SoundDefOf.MetalHitImportant.PlayOneShot(
                new TargetInfo(pawn.Position, pawn.Map));

            pawn.Drawer.Notify_DamageDeflected(
                new DamageInfo(__instance.def.projectile.damageDef, 0f));

            ThingDef projectileDef = __instance.def;
            __instance.Destroy(DestroyMode.Vanish);

            Projectile rebound = (Projectile)GenSpawn.Spawn(
                projectileDef,
                pawn.Position,
                pawn.Map);

            rebound.Launch(
                launcher: pawn,
                origin: pawn.Position.ToVector3Shifted(),
                usedTarget: new LocalTargetInfo(___launcher),
                intendedTarget: new LocalTargetInfo(___launcher),
                hitFlags: ProjectileHitFlags.All,
                preventFriendlyFire: false,
                equipment: pawn.equipment.Primary
            );

            return false;
        }
    }

    public class Thing_YoumuParryAnim : Thing
    {
        private const int TotalFrames = 14;
        private const int TicksPerFrame = 2;
        private new const float DrawSize = 1.6f;

        private int age;

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (age >= TotalFrames * TicksPerFrame)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = age / TicksPerFrame;
            if (frame >= TotalFrames) return;

            string texPath = $"Other/parry/spellBulletBa{frame:D3}";
            Material mat = MaterialPool.MatFrom(
                texPath,
                ShaderDatabase.Mote);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(DrawSize, 1f, DrawSize));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age");
        }
    }
}
