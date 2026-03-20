using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_RoyalFlareSun : Thing
    {
        public Pawn caster;
        private int age;

        private const int LifeTime = 120;
        private const int TotalFrames = 5;
        private const int TicksPerFrame = 3;
        private float drawScale = 2.2f;

        // ================= 粒子参数 =================
        private const int ParticlesPerTick = 1;
        private const float ParticleRadiusMin = 1.5f;
        private const float ParticleRadiusMax = 4.5f;
        // ============================================

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed || age >= LifeTime)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (Map == null) return;

            for (int i = 0; i < ParticlesPerTick; i++)
            {
                float angle = Rand.Range(0f, 360f);

                float radius = Mathf.Lerp(
                    ParticleRadiusMin,
                    ParticleRadiusMax,
                    Rand.Value * Rand.Value
                );

                Vector3 offset =
                    Quaternion.Euler(0f, angle, 0f) *
                    Vector3.forward * radius;

                Thing_RoyalFlareSunParticle particle =
                    (Thing_RoyalFlareSunParticle)ThingMaker.MakeThing(
                        ThingDef.Named("RoyalFlareSunParticle"));

                particle.Init(this, offset);

                GenSpawn.Spawn(particle, Position, Map);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = (age / TicksPerFrame) % TotalFrames;

            Material mat = MaterialPool.MatFrom(
                $"Projectiles/SUN/Sun_{frame}",
                ShaderDatabase.Transparent);

            Vector3 pos = caster.DrawPos + new Vector3(0f, 0f, 1.6f);
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, age * 3f, 0f),
                new Vector3(drawScale, 1f, drawScale));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0
            );
        }
    }
}
