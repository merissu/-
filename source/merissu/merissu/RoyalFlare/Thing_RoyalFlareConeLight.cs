using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_RoyalFlareConeLight : Thing
    {
        public Pawn caster;
        public Thing parentEmitter;
        public float angleOffset;
        private float angle;

        private const float RotateSpeed = 2.5f;
        private const float BaseDistance = 0.5f;
        private const float Height = 0.06f;
        private const float ConeLength = 20f;
        private const float ConeWidth = 5.5f;

        protected override void Tick()
        {
            base.Tick();
            if (caster == null || caster.Destroyed) { Destroy(); return; }
            angle += RotateSpeed;
            if (angle > 360f) angle -= 360f;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;
            Vector3 sunCenter = caster.DrawPos + new Vector3(0f, 0f, 1.6f);
            float totalAngle = angle + angleOffset;
            Vector3 directionOut = Quaternion.AngleAxis(totalAngle, Vector3.up) * Vector3.forward;
            Vector3 startPoint = sunCenter + directionOut * BaseDistance;
            Vector3 drawPos = startPoint + directionOut * (ConeLength / 2f);
            drawPos.y = AltitudeLayer.MoteLow.AltitudeFor() + Height;

            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, Quaternion.LookRotation(directionOut, Vector3.up), new Vector3(ConeWidth, 1f, ConeLength)), Graphic.MatSingle, 0);
        }
    }
}