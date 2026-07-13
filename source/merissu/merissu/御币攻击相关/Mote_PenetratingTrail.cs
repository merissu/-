using UnityEngine;
using Verse;

namespace merissu
{
    public class Mote_PenetratingTrail : Thing
    {
        public Vector3 exactPosition;
        public Quaternion spawnRotation = Quaternion.identity;
        private int age = 0;
        private const int MaxAge = 20;
        private const float StartScale = 4f;
        private const float EndScale = 0.2f;

        public override Vector3 DrawPos => exactPosition;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = age / (float)MaxAge;
            float scale = Mathf.Lerp(StartScale, EndScale, progress);
            float alpha = 1f - progress;

            drawLoc = exactPosition;
            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(
                def.graphicData.Graphic.MatSingle, alpha);
            Vector3 scaleVec = new Vector3(scale, 1f, scale);
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, spawnRotation, scaleVec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}