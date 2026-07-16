using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Graphic_ShinkiRecitation : Graphic
    {
        private Material[] frameMaterials;
        private Vector3 drawSize;

        public override Material MatSingle => frameMaterials[0];

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            color = data.color;
            colorTwo = data.colorTwo;
            drawSize = data.drawSize;

            var textures = ContentFinder<Texture2D>.GetAllInFolder("Other/ShinkiRecitation")
                .OrderBy(t => t.name)
                .ToList();

            if (textures.Count < 6)
            {
                Log.Error("ShinkiRecitation: 需要6张贴图（A0~A5），实际找到 " + textures.Count);
                frameMaterials = new Material[0];
                return;
            }

            frameMaterials = new Material[6];
            for (int i = 0; i < 6; i++)
            {
                frameMaterials[i] = MaterialPool.MatFrom(textures[i], ShaderDatabase.MoteGlow, color);
            }
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (frameMaterials == null || frameMaterials.Length == 0) return;

            Mote_ShinkiRecitation mote = thing as Mote_ShinkiRecitation;
            int frame = mote != null ? mote.currentFrame : 0;
            if (frame < 0 || frame >= frameMaterials.Length) frame = 0;

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(drawSize.x, 1f, drawSize.y)),
                frameMaterials[frame],
                0
            );
        }
    }
}