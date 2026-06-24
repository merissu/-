using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]

    public class Mote_ButterflyPetal : Thing
    {
        private int age;
        private Vector3 pos;
        private Vector3 vel;
        private float rot;
        private float rotVel;
        private float size;

        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override Vector3 DrawPos => pos;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            pos = this.Position.ToVector3Shifted();
            size = Rand.Range(0.8f, 1.6f);
            rot = Rand.Range(0f, 360f);

            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Rand.Range(0.03f, 0.07f);
            vel = new Vector3(Mathf.Cos(angle) * spd, 0, Mathf.Sin(angle) * spd);
            vel.z += Rand.Range(0.02f, 0.04f);
            rotVel = Rand.Range(-10f, 10f);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            pos += vel;
            vel.z -= 0.002f;
            vel.x *= 0.97f;
            rot += rotVel;
            rotVel *= 0.99f;

            if (age >= 80)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (this.Graphic == null)
                return;

            float alpha = 1f - (float)age / 80f;
            propBlock.SetColor(ShaderPropertyIDs.Color, new Color(1, 1, 1, alpha));

            Vector3 renderPos = pos;
            renderPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.05f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                renderPos,
                Quaternion.AngleAxis(rot, Vector3.up),
                new Vector3(size, 1, size)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, this.Graphic.MatSingle, 0, null, 0, propBlock);
        }
    }
}