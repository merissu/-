using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_XWaveLaser : Thing
    {
        public Pawn caster;

        private int age;

        private const int LifeTime = 90;
        private const int FadeInTicks = 10;
        private const int FadeOutTicks = 10;

        private float angle;
        private float rotateSpeed;
        private float radius;

        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            angle = Rand.Range(0f, 360f);
            rotateSpeed = Rand.Range(-6f, 6f);
            radius = Rand.Range(3f, 5f);

            mat = MaterialPool.MatFrom(
                "Other/bulletCe000",
                ShaderDatabase.MoteGlow
            );
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Dead || !caster.Spawned)
            {
                Destroy();
                return;
            }

            angle += rotateSpeed;
            Position = caster.Position;

            if (age >= LifeTime)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            float alpha = 1f;

            if (age < FadeInTicks)
            {
                alpha = age / (float)FadeInTicks;
            }
            else if (age > LifeTime - FadeOutTicks)
            {
                alpha = (LifeTime - age) / (float)FadeOutTicks;
            }

            Color color = Color.white;
            color.a = alpha;
            propBlock.SetColor("_Color", color);

            Vector3 center = caster.DrawPos;
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * radius,
                0f,
                Mathf.Sin(rad) * radius
            );

            Vector3 pos = center + offset;

            Vector3 dir = (pos - center).normalized;
            Quaternion rot =
                Quaternion.LookRotation(dir) *
                Quaternion.Euler(0f, -90f, 0f);

            float length = 18f; 
            float width = 2.5f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                rot,
                new Vector3(length, 1f, width)
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
