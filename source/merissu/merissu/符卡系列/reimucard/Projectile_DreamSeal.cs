using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Projectile_DreamSeal : Projectile
    {
        private static Material cachedGlowMat;
        private static Material GlowMat
        {
            get
            {
                if (cachedGlowMat == null)
                {
                    cachedGlowMat = MaterialPool.MatFrom("Other/Glow", ShaderDatabase.MoteGlow);
                }
                return cachedGlowMat;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            Color glowColor = Color.white;
            if (def.defName.Contains("Red")) glowColor = new Color(1f, 0.2f, 0.2f, 0.6f);
            else if (def.defName.Contains("Green")) glowColor = new Color(0.2f, 1f, 0.2f, 0.6f);
            else if (def.defName.Contains("Blue")) glowColor = new Color(0.2f, 0.2f, 1f, 0.6f);

            Material coloredGlowMat = MaterialPool.MatFrom((Texture2D)GlowMat.mainTexture, GlowMat.shader, glowColor);

            float scale = 3f;
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawLoc + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(scale, 1f, scale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, coloredGlowMat, 0);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            IntVec3 pos = hitThing != null ? hitThing.Position : Position;

            GenExplosion.DoExplosion(
                pos,
                Map,
                2f,
                DamageDefOf.Bomb,
                launcher,
                damAmount: 400);

            base.Impact(hitThing, blockedByShield);
        }
    }
}