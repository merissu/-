using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_RoyalFlareSunParticle : Thing
    {
        private Thing_RoyalFlareSun sun;
        private Vector3 offsetFromSun;  
        private int age;
        private const int LifeTime = 50;
        private float speed = 0.05f; 
        private float scale = 0.5f;

        public void Init(Thing_RoyalFlareSun sunInstance, Vector3 initialOffset)
        {
            sun = sunInstance;
            offsetFromSun = initialOffset;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (sun == null || sun.Destroyed || age >= LifeTime)
            {
                Destroy();
                return;
            }

            offsetFromSun = Vector3.Lerp(offsetFromSun, Vector3.zero, speed);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (sun == null) return;

            Vector3 center = sun.caster.DrawPos + new Vector3(0f, 0f, 1.6f);
            Vector3 pos = center + offsetFromSun;
            pos.y = AltitudeLayer.MoteLow.AltitudeFor();

            float alpha = 1f - (age / (float)LifeTime);

            Material mat = Graphic.MatSingle;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, block);
        }
    }
}
