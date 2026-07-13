using UnityEngine;
using Verse;

namespace merissu
{
    public class Mote_VigilanceShockwave : Thing
    {
        public Vector3 exactPosition;
        public float rotationAngle;
        private int age;
        private const int MaxAge = 12;
        private const float StartScale = 0.3f;
        private const float EndScale = 0.8f;

        public override Vector3 DrawPos => exactPosition;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = age / (float)MaxAge;
            float scale = Mathf.Lerp(StartScale, EndScale, progress);
            float alpha = 1f - progress * progress;   

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(def.graphicData.Graphic.MatSingle, alpha);
            Quaternion rot = Quaternion.Euler(0f, rotationAngle, 0f);
            Vector2 baseSize = def.graphicData.drawSize;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, new Vector3(baseSize.x * scale, 1f, baseSize.y * scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}