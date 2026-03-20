using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_RoyalFlareShockwaveParticle : Thing
    {
        private Vector3 position;
        private Vector3 velocity;
        private int age;

        private const int LifeTime = 40;
        private const float StartSpeed = 0.45f;
        private const float Drag = 0.92f;
        private const float StartScale = 0.8f;

        public void Init(Vector3 origin)
        {
            position = origin;

            float angle = Rand.Range(0f, 360f);
            Vector3 dir =
                Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            velocity = dir * Rand.Range(StartSpeed * 0.7f, StartSpeed * 1.3f);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            position += velocity;
            velocity *= Drag;

            if (age >= LifeTime)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - age / (float)LifeTime;
            float scale = StartScale * Mathf.Lerp(1.2f, 0.4f, age / (float)LifeTime);

            Vector3 pos = position;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = Graphic.MatSingle;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                Vector3.one * scale
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0,
                null,
                0,
                block
            );
        }
    }
}
