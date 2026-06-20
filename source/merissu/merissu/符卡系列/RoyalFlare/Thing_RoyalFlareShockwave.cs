using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_RoyalFlareShockwave : Thing
    {
        public Pawn caster;
        private int age;
        private const int LifeTime = 15;
        private const float StartRadius = 0.5f;
        private const float EndRadius = 20f;

        private static readonly Material ShockwaveMat = MaterialPool.MatFrom("Other/Shockwave", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (caster == null || age >= LifeTime) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;
            float t = (float)age / LifeTime;
            float radius = Mathf.Lerp(StartRadius, EndRadius, t);
            Color c = Color.white; c.a = 1f - t;
            propBlock.SetColor(ShaderPropertyIDs.Color, c);

            Vector3 pos = caster.DrawPos + new Vector3(0f, 0f, 1.6f);
            pos.y = AltitudeLayer.MoteLow.AltitudeFor() + 0.04f;

            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(radius * 2f, 1f, radius * 2f)), ShockwaveMat, 0, null, 0, propBlock);
        }
    }
}