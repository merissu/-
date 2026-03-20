using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_XWaveRing : Thing
    {
        public Pawn caster;
        private int age;

        private const int FadeInTicks = 10;
        private const int FadeOutTicks = 20;
        private const int LifeTime = FadeInTicks + FadeOutTicks;

        private const float StartRadius = 1f;
        private const float MidRadius = 12f;
        private const float EndRadius = 13f;

        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom(
                def.graphicData.texPath,
                ShaderDatabase.MoteGlow
            );
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
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float radius;
            float alpha;

            if (age <= FadeInTicks)
            {
                float t = age / (float)FadeInTicks;
                radius = Mathf.Lerp(StartRadius, MidRadius, t);
                alpha = t;
            }
            else
            {
                float t = (age - FadeInTicks) / (float)FadeOutTicks;
                radius = Mathf.Lerp(MidRadius, EndRadius, t);
                alpha = 1f - t;
            }

            Color color = Color.white;
            color.a = alpha;
            propBlock.SetColor("_Color", color);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(radius * 2f, 1f, radius * 2f)
            );

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
