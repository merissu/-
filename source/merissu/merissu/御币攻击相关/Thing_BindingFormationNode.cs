using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_BindingFormationNode : Thing
    {
        public Thing_BindingFormationController controller;
        public Vector3 startPos;
        public Pawn targetPawn;
        public int startTick;
        public int warmupTicks;

        public Vector3 exactPosition;
        public Quaternion nodeRotation = Quaternion.identity;

        private int frameCounter;

        private const float HalfLength = 2.0f;
        private const float HalfWidth = 1.0f;

        public override Vector3 DrawPos => exactPosition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            frameCounter = 0;
        }

        protected override void Tick()
        {
            base.Tick();
            if (controller == null || controller.Destroyed || targetPawn == null || targetPawn.Destroyed)
            {
                Destroy();
                return;
            }

            float progress = Mathf.Clamp01((float)(Find.TickManager.TicksGame - startTick) / warmupTicks);
            exactPosition = Vector3.Lerp(startPos, targetPawn.DrawPos, progress);
            Position = exactPosition.ToIntVec3();

            frameCounter++;
            if (frameCounter >= 60) frameCounter = 0;

            InterceptBullets();
        }

        private void InterceptBullets()
        {
            if (Map == null || controller == null) return;
            List<Thing> projectiles = Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Thing projThing = projectiles[i];
                if (projThing == null || projThing.Destroyed) continue;

                Thing launcher = null;
                if (projThing is Projectile vanillaProj)
                    launcher = vanillaProj.launcher;
                else if (Thing_VigilanceFormation.ceProjectileType != null &&
                         Thing_VigilanceFormation.ceProjectileType.IsAssignableFrom(projThing.GetType()))
                    launcher = Thing_VigilanceFormation.GetCasterFromCEProjectile(projThing);
                else continue;

                if (launcher == null || launcher.Faction == null ||
                    !launcher.Faction.HostileTo(controller.caster.Faction))
                    continue;

                Vector3 localPos = Quaternion.Inverse(nodeRotation) * (projThing.DrawPos - exactPosition);
                if (Mathf.Abs(localPos.x) <= HalfWidth && Mathf.Abs(localPos.z) <= HalfLength)
                    projThing.Destroy();
            }
        }

        public bool IsCollidingWithTarget(Thing target)
        {
            Vector3 localPos = Quaternion.Inverse(nodeRotation) * (target.DrawPos - exactPosition);
            return Mathf.Abs(localPos.x) <= HalfWidth && Mathf.Abs(localPos.z) <= HalfLength;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            int frameIndex = (frameCounter / 2) % 30;
            Material mat = VigilanceFormationGraphics.Frames[frameIndex];

            Vector3 scale = new Vector3(HalfWidth * 2f, 1f, HalfLength * 2f);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, nodeRotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}