using Verse;
using UnityEngine;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Mote_HisoutenAnimated : MoteThrown
    {
        private static Material[] cachedMats;

        public float myScale = 1f;
        public int randOffset = 0;

        protected static void DoubleCheckMats()
        {
            if (cachedMats == null)
            {
                cachedMats = new Material[4];
                for (int i = 0; i < 4; i++)
                {
                    cachedMats[i] = MaterialPool.MatFrom("Projectiles/Hisouten/Hisouten_00" + i, ShaderDatabase.MoteGlow);
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DoubleCheckMats();

            int frame = ((Find.TickManager.TicksGame + randOffset) / 6) % 4;
            Material baseMat = cachedMats[frame];

            float alpha = this.Alpha;
            if (alpha <= 0.001f) return;
            Material finalMat = FadedMaterialPool.FadedVersionOf(baseMat, alpha);

            Vector3 scaleVec = new Vector3(myScale, 1f, myScale);

            Quaternion q = Quaternion.AngleAxis(this.exactRotation, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, q, scaleVec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, finalMat, 0);
        }
    }
}