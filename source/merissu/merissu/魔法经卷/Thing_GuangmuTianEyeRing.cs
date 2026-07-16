using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_GuangmuTianEyeRing : Thing
    {
        public Pawn caster;

        private int age;
        private const int FadeInTicks = 15;
        private const int FadeOutTicks = 15;
        private int LifeTime => FadeInTicks + FadeOutTicks;

        private const float StartRadius = 0.5f;
        private const float EndRadius = 50f;  

        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (age >= LifeTime)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (mat == null) return;

            float t = age / (float)LifeTime;
            float radius = Mathf.Lerp(StartRadius, EndRadius, t);

            float alpha;
            if (age <= FadeInTicks)
            {
                alpha = age / (float)FadeInTicks;
            }
            else
            {
                alpha = 1f - (age - FadeInTicks) / (float)FadeOutTicks;
            }
            alpha = Mathf.Clamp01(alpha);

            propBlock.SetColor("_Color", new Color(1, 1, 1, alpha));

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector2 drawSize = def.graphicData.drawSize;
            float scaleX = drawSize.x;
            float scaleZ = drawSize.y;

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(radius * scaleX * 2f, 1f, radius * scaleZ * 2f)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, propBlock);
        }
    }
}