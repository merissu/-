using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class GateShockwaveGraphics
    {
        public static readonly Material RingMat = MaterialPool.MatFrom("Weapons/Bullet/spellBulletAb000", ShaderDatabase.MoteGlow);
        public static readonly Material FlashMat = MaterialPool.MatFrom("Weapons/Bullet/spellBulletA000", ShaderDatabase.MoteGlow);
    }

    public class Thing_GateShockwave : Thing
    {
        private int age = 0;
        private const int MaxAge = 6;
        public Vector3 exactPosition;

        public override Vector3 DrawPos => (exactPosition != Vector3.zero) ? exactPosition : base.DrawPos;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (exactPosition == Vector3.zero) exactPosition = this.Position.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float scale = Mathf.Lerp(1f, 5f, progress);
            float alpha = 1f - progress;

            drawLoc = this.DrawPos;
            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material ringMat = FadedMaterialPool.FadedVersionOf(GateShockwaveGraphics.RingMat, alpha);
            Matrix4x4 ringMatrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, ringMatrix, ringMat, 0);

            Material flashMat = FadedMaterialPool.FadedVersionOf(GateShockwaveGraphics.FlashMat, alpha);
            Matrix4x4 flashMatrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, Vector3.one);
            Graphics.DrawMesh(MeshPool.plane10, flashMatrix, flashMat, 0);
        }
    }

    public class CompProperties_ImpactShockwave : CompProperties
    {
        public ThingDef shockwaveDef;

        public CompProperties_ImpactShockwave()
        {
            this.compClass = typeof(CompImpactShockwave);
        }
    }

    public class CompImpactShockwave : ThingComp
    {
        public CompProperties_ImpactShockwave Props => (CompProperties_ImpactShockwave)this.props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap != null && Props.shockwaveDef != null)
            {
                Vector3 spawnPos = this.parent.DrawPos;
                Thing shockwave = ThingMaker.MakeThing(Props.shockwaveDef);
                GenSpawn.Spawn(shockwave, this.parent.Position, previousMap);

                if (shockwave is Thing_GateShockwave sw)
                {
                    sw.exactPosition = spawnPos;
                }
            }
        }
    }
}