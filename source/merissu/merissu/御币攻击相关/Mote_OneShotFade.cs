using UnityEngine;
using Verse;

namespace merissu
{
    public class Mote_OneShotFade : Thing
    {
        public Vector3 exactPosition;
        public int ticksLeft = 25;
        public Quaternion rotation = Quaternion.identity;

        public override Vector3 DrawPos => exactPosition;

        protected override void Tick()
        {
            base.Tick();
            ticksLeft--;
            if (ticksLeft <= 0) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = Mathf.Clamp01(ticksLeft / 25f);
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Material mat = FadedMaterialPool.FadedVersionOf(def.graphicData.Graphic.MatSingle, alpha);
            Vector2 size = def.graphicData.drawSize;
            Vector3 scale = new Vector3(size.y, 1f, size.x); 
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}