using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_RoyalFlareRangeIndicator : Thing
    {
        private Pawn caster;
        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public void Init(Pawn pawn) { caster = pawn; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.Transparent);
        }

        protected override void Tick()
        {
            base.Tick();
            if (caster == null || caster.Destroyed) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;
            Color c = Color.white; c.a = 0.8f;
            propBlock.SetColor(ShaderPropertyIDs.Color, c);

            Vector3 pos = caster.DrawPos + new Vector3(0f, 0f, 1.6f);
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(80f, 1f, 80f)), mat, 0, null, 0, propBlock);
        }
    }
}