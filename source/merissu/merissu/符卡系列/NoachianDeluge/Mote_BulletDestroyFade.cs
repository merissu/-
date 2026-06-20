using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Mote_BulletDestroyFade : Thing
    {
        public Vector3 exactPosition;

        private int age;

        private const int LifeTime = 20;

        private struct SplashParticle
        {
            public Vector3 pos;
            public Vector3 vel;
            public float size;
            public float rotation;
        }

        private readonly List<SplashParticle> particles = new List<SplashParticle>();
        private static readonly Material Mat =
            MaterialPool.MatFrom(
                "Other/bulletBb003",
                ShaderDatabase.MoteGlow);

        private static readonly MaterialPropertyBlock PB =
            new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (respawningAfterLoad)
                return;

            int count = Rand.RangeInclusive(6, 10);

            for (int i = 0; i < count; i++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;

                float speed = Rand.Range(0.05f, 0.18f);

                SplashParticle p = new SplashParticle
                {
                    pos = exactPosition,
                    vel = new Vector3(
                        Mathf.Cos(angle) * speed,
                        0f,
                        Mathf.Sin(angle) * speed),

                    size = Rand.Range(0.25f, 0.55f),

                    rotation = Rand.Range(0f, 360f)
                };

                particles.Add(p);
            }
        }

        protected override void Tick()
        {
            age++;

            for (int i = 0; i < particles.Count; i++)
            {
                SplashParticle p = particles[i];

                p.pos += p.vel;

                p.vel *= 0.92f;

                particles[i] = p;
            }

            if (age >= LifeTime)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - (float)age / LifeTime;

            PB.SetColor(
                "_Color",
                new Color(1f, 1f, 1f, alpha));

            for (int i = 0; i < particles.Count; i++)
            {
                SplashParticle p = particles[i];

                Vector3 pos = p.pos;
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                Matrix4x4 matrix =
                    Matrix4x4.TRS(
                        pos,
                        Quaternion.AngleAxis(
                            p.rotation,
                            Vector3.up),
                        new Vector3(
                            p.size,
                            1f,
                            p.size));

                Graphics.DrawMesh(
                    MeshPool.plane10,
                    matrix,
                    Mat,
                    0,
                    null,
                    0,
                    PB);
            }
        }
    }
}