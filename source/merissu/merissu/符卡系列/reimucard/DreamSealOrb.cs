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
        public int spawnDelay;

        private int currentAge;
        private const int AppearDuration = 30;
        private const int OrbitDuration = 120;
        private const float OrbitRadius = 1.8f;
        private static readonly Material GlowMat = MaterialPool.MatFrom("Other/Glow", ShaderDatabase.MoteGlow);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref target, "target");
            Scribe_Values.Look(ref angleOffset, "angleOffset");
            Scribe_Values.Look(ref spawnDelay, "spawnDelay");
            Scribe_Values.Look(ref currentAge, "currentAge");
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (currentAge < spawnDelay) return;

            float progress = Mathf.Clamp01((currentAge - spawnDelay) / (float)AppearDuration);
            float scale = 3f * progress;

            if (Graphic != null)
            {
                Matrix4x4 baseMatrix = default;
                baseMatrix.SetTRS(drawLoc, Quaternion.identity, new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, baseMatrix, Graphic.MatSingle, 0);
            }

            Color glowColor = Color.white;
            if (def.defName.Contains("Red")) glowColor = new Color(1f, 0.2f, 0.2f, 0.6f);
            else if (def.defName.Contains("Green")) glowColor = new Color(0.2f, 1f, 0.2f, 0.6f);
            else if (def.defName.Contains("Blue")) glowColor = new Color(0.2f, 0.2f, 1f, 0.6f);

            Material coloredGlowMat = MaterialPool.MatFrom((Texture2D)GlowMat.mainTexture, GlowMat.shader, glowColor);

            Matrix4x4 glowMatrix = default;
            glowMatrix.SetTRS(drawLoc + new Vector3(0f, -0.01f, 0f), Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, glowMatrix, coloredGlowMat, 0);
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || caster.Destroyed)
            {
                Destroy();
                return;
            }

            currentAge++;

            if (currentAge >= spawnDelay + AppearDuration + OrbitDuration)
            {
                Fire();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (caster == null) return base.DrawPos;

                if (currentAge < spawnDelay) return caster.DrawPos;

                float progress = Mathf.Clamp01((currentAge - spawnDelay) / (float)AppearDuration);
                float currentRadius = OrbitRadius * progress;

                float angle = (currentAge * 4f + angleOffset) % 360f;

                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * currentRadius;

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