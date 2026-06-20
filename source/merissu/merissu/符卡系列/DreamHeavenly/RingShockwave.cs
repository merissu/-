using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class RingShockwave : Thing
    {
        public Pawn caster;
        private int age;
        private const int LifeTime = 30;
        private const float StartRadius = 0.5f;
        private const float EndRadius = 40f;
        private Material mat;

        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom(
                def.graphicData.texPath,
                ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Dead)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (age >= LifeTime)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float t = (float)age / LifeTime;
            float radius = Mathf.Lerp(StartRadius, EndRadius, t);

            float alpha = 1f - t;

            Color color = Color.white;
            color.a = alpha;
            propBlock.SetColor("_Color", color);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(radius * 2f, 1f, radius * 2f));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0,      
                null,   
                0,      
                propBlock 
            );
        }
    }
}