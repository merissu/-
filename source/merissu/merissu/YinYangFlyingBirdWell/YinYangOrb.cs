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

            Mesh mesh = MeshPool.plane10;
            Material mat = Graphic.MatSingle;

            Vector3 scale = new Vector3(
                def.graphicData.drawSize.x,
                1f,
                def.graphicData.drawSize.y
            );

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc,
                Quaternion.AngleAxis(comp.RotationAngle, Vector3.up),
                scale
            );

            Graphics.DrawMesh(mesh, matrix, mat, 0);
        }
    }
}
