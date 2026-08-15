using Verse;
using UnityEngine;

namespace merissu
{
    public class Thing_CorollaVisionTrail : Thing
    {
        private float alpha = 1f;
        private const float FadeTicks = 60f;
        private float rotation;
        private float meshSize;
        private Vector3 realExactPos; 

        public void Init(Vector3 pos, float rot, float radius)
        {
            this.Position = pos.ToIntVec3();
            this.realExactPos = pos;
            this.rotation = rot;
            this.meshSize = radius * 2f;
        }

        protected override void Tick()
        {
            base.Tick();
            alpha -= 1f / FadeTicks;
            if (alpha <= 0f) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = realExactPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.Euler(0f, rotation, 0f),
                new Vector3(meshSize, 1f, meshSize));

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, mpb);
        }
    }
}