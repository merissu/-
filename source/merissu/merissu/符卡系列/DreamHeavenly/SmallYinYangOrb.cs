using Verse;
using UnityEngine;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class SmallYinYangOrb : Thing
    {
        public Pawn caster;
        public Thing parentEmitter;

        public float angleOffset;
        private float age;
        private float spinAngle;

        private const float OrbitRadius = 2.2f;
        private const float OrbitSpeed = 15f;
        private const float SpinSpeed = 15f;

        private static readonly Material BlueAuraMat =
            MaterialPool.MatFrom(
                "Other/BlueAura",
                ShaderDatabase.MoteGlow);

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Dead || parentEmitter == null || parentEmitter.Destroyed)
            {
                Destroy();
                return;
            }

            spinAngle += SpinSpeed;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float angle = angleOffset + age * OrbitSpeed;

            Vector3 offset =
                Quaternion.Euler(0, angle, 0) * Vector3.forward * OrbitRadius;

            Vector3 drawPos = caster.DrawPos + offset;

            DrawBlueAura(drawPos);

            Graphic.Draw(
                drawPos,
                Rot4.FromAngleFlat(spinAngle),
                this);
        }

        private void DrawBlueAura(Vector3 drawPos)
        {
            Vector3 pos = drawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.08f;

            float t = Find.TickManager.TicksGame * 0.05f;
            float size = 1.4f + Mathf.Sin(t) * 0.35f;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, 0.2f));

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.identity,
                new Vector3(size, 1f, size));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                BlueAuraMat,
                0);
        }
    }
}
