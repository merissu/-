using Verse;
using UnityEngine;

namespace merissu
{
    public class Thing_NightFogKnifeBlade : Thing
    {
        public Thing_NightFogKnifeController controller;
        public Vector3 fixedOffset;
        public int index;

        private float spin;

        protected override void Tick()
        {
            base.Tick();

            spin -= 10f;

            if (controller == null || controller.caster == null || controller.Destroyed)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (controller == null || controller.caster == null || Graphic == null)
                return;

            float finalSpin = spin + index * 30f;

            Vector3 drawPos = controller.caster.DrawPos + fixedOffset;
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(spin + index * 30f, Vector3.up),
                def.graphicData.drawSize.ToVector3()
            );

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                Graphic.MatSingle,
                0
            );
        }
    }
}
