using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class CompProperties_YinYangRotatingGraphicblue : CompProperties
    {
        public CompProperties_YinYangRotatingGraphicblue()
        {
            this.compClass = typeof(Comp_YinYangRotatingGraphicblue);
        }
    }
    public class Comp_YinYangRotatingGraphicblue : ThingComp
    {
        private Material matLayer0;
        private Material matLayer1;
        private Material matLayer2;
        private Material matLayer3;

        private static readonly Vector3 Offset = new Vector3(0, 0.01f, 0);

        public override void PostDraw()
        {
            base.PostDraw();

            Vector3 center = parent.DrawPos;
            float drawSize = 1.5f;
            Mesh mesh = MeshPool.GridPlane(new Vector2(drawSize, drawSize));

            float ticks = Find.TickManager.TicksGame;

            float slowRotation = ticks * 3f;
            float fastRotation = ticks * 6f;
            float superFastRotation = ticks * 9f;

            if (matLayer0 == null) InitializeMaterials();

            Graphics.DrawMesh(mesh, center, Quaternion.Euler(0, -fastRotation, 0), matLayer3, 0);

            Graphics.DrawMesh(mesh, center + Offset, Quaternion.Euler(0, -slowRotation, 0), matLayer2, 0);

            Graphics.DrawMesh(mesh, center + Offset * 2, Quaternion.Euler(0, superFastRotation, 0), matLayer1, 0);

            Graphics.DrawMesh(mesh, center + Offset * 3, Quaternion.identity, matLayer0, 0);
        }
        private void InitializeMaterials()
        {
            matLayer0 = MaterialPool.MatFrom("Pawn/YinYangJade/blue0", ShaderDatabase.Mote);
            matLayer1 = MaterialPool.MatFrom("Pawn/YinYangJade/blue1", ShaderDatabase.MoteGlow);
            matLayer2 = MaterialPool.MatFrom("Pawn/YinYangJade/blue2", ShaderDatabase.MoteGlow);
            matLayer3 = MaterialPool.MatFrom("Pawn/YinYangJade/blue3", ShaderDatabase.MoteGlow);
        }
    }
}