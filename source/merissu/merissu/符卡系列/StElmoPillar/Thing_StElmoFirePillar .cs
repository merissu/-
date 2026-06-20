using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_StElmoFirePillar : Thing
    {
        private int age;
        private const int TicksPerFrame = 2;
        private const int TotalFrames = 25;
        private float drawScale = 8.0f;

        private const int FadeOutTicks = 20;

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (age > TicksPerFrame * TotalFrames)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Min(age / TicksPerFrame, TotalFrames - 1);

            Material mat = MaterialPool.MatFrom(
                $"Other/StElmoFirePillar/pillar_{frame}",
                ShaderDatabase.MoteGlow);

            float alpha = 1f;
            int totalLifespan = TicksPerFrame * TotalFrames;
            int ticksLeft = totalLifespan - age;

            if (ticksLeft <= FadeOutTicks)
            {
                alpha = (float)ticksLeft / FadeOutTicks;
            }

            Material fadedMat = FadedMaterialPool.FadedVersionOf(mat, alpha);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(drawScale, 1f, drawScale));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                fadedMat,
                0);
        }
    }
}