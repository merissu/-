using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class VigilanceFormationGraphics
    {
        public static readonly Material[] Frames;
        static VigilanceFormationGraphics()
        {
            Frames = new Material[30];
            for (int i = 0; i < 30; i++)
                Frames[i] = MaterialPool.MatFrom(
                    $"Projectiles/REIMU/VigilanceFormation/bulletDa{i:D3}",
                    ShaderDatabase.MoteGlow);
        }
    }

    public class Thing_VigilanceFormation : Thing
    {
        private static Type ceProjectileType = null;

        static Thing_VigilanceFormation()
        {
            if (ModLister.HasActiveModWithName("Combat Extended"))
            {
                ceProjectileType = Type.GetType("CombatExtended.ProjectileCE, CombatExtended");
                if (ceProjectileType == null)
                    ceProjectileType = Type.GetType("CombatExtended.BulletCE, CombatExtended");
                if (ceProjectileType == null)
                    Log.Warning("ce未找到兼容的子弹类型。");
                else
                    Log.Message("ce兼容");
            }
        }

        public Vector3 exactPosition;
        public Vector3 aimDirection;
        public Faction faction;
        public int ticksLeft = 360; 
        private int frameCounter;

        private const float Height = 6f;   
        private const float Width = 3f;

        private const float RotationOffset = 90f;

        private int FadeOutDuration => 60; 

        public override Vector3 DrawPos => exactPosition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            frameCounter = 0;
        }

        protected override void Tick()
        {
            base.Tick();
            ticksLeft--;
            if (ticksLeft <= 0)
            {
                Destroy();
                return;
            }

            frameCounter++;
            if (frameCounter >= 30) frameCounter = 0;

            InterceptBullets();
        }

        private void InterceptBullets()
        {
            if (Map == null || faction == null) return;
            List<Thing> projectiles = Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Thing projThing = projectiles[i];
                if (projThing == null || projThing.Destroyed) continue;

                Thing launcher = null;
                if (projThing is Projectile vanillaProj)
                    launcher = vanillaProj.launcher;
                else if (ceProjectileType != null && ceProjectileType.IsAssignableFrom(projThing.GetType()))
                    launcher = GetCasterFromCEProjectile(projThing);
                else
                    continue;

                if (launcher == null) continue;

                if (launcher.Faction == null || !launcher.Faction.HostileTo(faction))
                    continue;

                Vector3 projPos = projThing.DrawPos;

                Vector3 localPos = Quaternion.Inverse(Quaternion.LookRotation(aimDirection)) * (projPos - exactPosition);
                if (Mathf.Abs(localPos.x) <= Width / 2f && Mathf.Abs(localPos.z) <= Height / 2f)
                {
                    if (Vector3.Dot((exactPosition - projPos).normalized, aimDirection) > 0.2f)
                    {
                        projThing.Destroy();
                    }
                }
            }
        }

        private static Thing GetCasterFromCEProjectile(Thing ceProj)
        {
            try
            {
                PropertyInfo prop = ceProj.GetType().GetProperty("Launcher");
                if (prop != null)
                    return prop.GetValue(ceProj) as Thing;

                FieldInfo field = ceProj.GetType().GetField("launcher");
                if (field != null)
                    return field.GetValue(ceProj) as Thing;
            }
            catch { }
            return null;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f;
            if (ticksLeft < FadeOutDuration)
                alpha = (float)ticksLeft / FadeOutDuration;

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Material mat = FadedMaterialPool.FadedVersionOf(VigilanceFormationGraphics.Frames[frameCounter], alpha);

            Quaternion rot = Quaternion.LookRotation(-aimDirection);
            rot *= Quaternion.Euler(0f, RotationOffset, 0f);

            Vector3 scale = new Vector3(Width, 1f, Height);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}