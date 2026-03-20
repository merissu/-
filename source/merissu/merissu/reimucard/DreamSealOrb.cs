using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class DreamSealOrb : ThingWithComps
    {
        public Pawn caster;
        public Thing target;

        public float angleOffset;
        public int fireDelayTicks;

        private int age;
        private const int OrbitDuration = 120;
        private const float OrbitRadius = 1.8f;
        private static readonly Material GlowMat = MaterialPool.MatFrom("Other/Glow", ShaderDatabase.MoteGlow);

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
        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed)
            {
                Destroy();
                return;
            }

            if (age == OrbitDuration + fireDelayTicks)
            {
                Fire();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (caster == null) return base.DrawPos;

                float angle = (age * 4f + angleOffset) % 360f;
                Vector3 offset =
                    Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward * OrbitRadius;

                return caster.DrawPos + offset;
            }
        }

        private void Fire()
        {
            if (target == null || target.Destroyed) { Destroy(); return; }

            string projectileDefName = this.def.defName.Replace("Orb", "Projectile");

            Projectile proj = (Projectile)GenSpawn.Spawn(
                ThingDef.Named(projectileDefName),
                this.Position,
                Map);

            proj.Launch(caster, this.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);

            Destroy();
        }
    }
}
