using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_StarlightLaserParticle : Thing
    {
        private Vector3 position;
        private Vector3 velocity;
        private Quaternion rotation; 
        private int age;

        private const int LifeTime = 30;

        private const float StartSpeed = 0.3f; 
        private const float Drag = 0.8f;        

        private const float StartWidth = 1.5f;  
        private const float Length = 1.5f;      

        public void Init(Vector3 origin)
        {
            position = origin;

            float angle = Rand.Range(0f, 360f);
            Vector3 dir =
                Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            velocity = dir * Rand.Range(
                StartSpeed * 0.7f,
                StartSpeed * 1.3f
            );

            rotation = Quaternion.LookRotation(dir);
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
            float t = age / (float)LifeTime;

            float width = Mathf.Lerp(StartWidth, 0f, t);

            float alpha = 1f - t;

            Vector3 pos = position;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = Graphic.MatSingle;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                rotation,                 
                new Vector3(width, 1f, Length)
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
