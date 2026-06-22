using UnityEngine;
using Verse;

namespace merissu
{
    public class YinYangOrb : ThingWithComps
    {
        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = base.DrawPos;
                pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                Comp_YinYangOrb comp = this.TryGetComp<Comp_YinYangOrb>();
                if (comp != null)
                {
                    pos.x = comp.VisualPos.x;
                    pos.z = comp.VisualPos.z;
                }
                return pos;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Comp_YinYangOrb comp = this.TryGetComp<Comp_YinYangOrb>();
            if (comp == null)
            {
                base.DrawAt(drawLoc, flip);
                return;
            }

            float scale = comp.CurrentScale;
            float alpha = comp.CurrentAlpha;
            if (scale <= 0f || alpha <= 0f) return;

            Mesh mesh = MeshPool.plane10;
            Material baseMat = Graphic.MatSingle;
            Material mat = FadedMaterialPool.FadedVersionOf(baseMat, alpha);

            Vector3 finalScale = new Vector3(
                def.graphicData.drawSize.x * scale,
                1f,
                def.graphicData.drawSize.y * scale
            );

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc,
                Quaternion.AngleAxis(comp.RotationAngle, Vector3.up),
                finalScale
            );

            Graphics.DrawMesh(mesh, matrix, mat, 0);
        }
    }
}