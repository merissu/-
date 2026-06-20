using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Projectile_StElmoFireball : Projectile
    {
        protected override void Tick()
        {
            base.Tick();

            if (Map == null) return;

            if (Find.TickManager.TicksGame % 1 != 0) return;

            Thing_StElmoFireTrailParticle particle = (Thing_StElmoFireTrailParticle)ThingMaker.MakeThing(
                ThingDef.Named("StElmoFireTrailParticle"));

            float randomAngle = Rand.Range(0f, 360f);
            float initialSpeed = Rand.Range(0.02f, 0.08f);
            float scale = Rand.Range(1f, 1.4f); 

            particle.Setup(ExactPosition, randomAngle, initialSpeed, scale);

            GenSpawn.Spawn(particle, ExactPosition.ToIntVec3(), Map);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 pos = Position;

            base.Impact(hitThing, blockedByShield);

            GenExplosion.DoExplosion(
                pos, map, 3.5f, DamageDefOf.Flame, launcher, damAmount: 1000);

            GenSpawn.Spawn(
                ThingDef.Named("StElmoFirePillar"), pos, map);
        }
    }
    public class Thing_StElmoFireTrailParticle : Thing
    {
        private int age = 0;

        public Vector3 exactPosition;
        public Vector3 velocity;
        public float drawScale = 1.0f;

        private const int TicksPerFrame = 2; 
        private const int TotalFrames = 8;   
        private const float Gravity = 0.005f; 
        private const float Drag = 0.95f;    
        public void Setup(Vector3 startPos, float angle, float speed, float scale)
        {
            exactPosition = startPos;
            drawScale = scale;

            velocity = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * speed,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * speed
            );
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (age >= TicksPerFrame * TotalFrames)
            {
                Destroy();
                return;
            }

            exactPosition += velocity;

            velocity.x *= Drag;
            velocity.z -= Gravity;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Clamp(age / TicksPerFrame, 0, TotalFrames - 1);

            string texPath = $"Other/StElmoFireTrail/bulletJa00{frame}";
            Material mat = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow);

            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix =
                Matrix4x4.TRS(
                    pos,
                    Quaternion.identity,
                    new Vector3(drawScale, 1f, drawScale));

            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                mat,
                0);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref velocity, "velocity");
            Scribe_Values.Look(ref drawScale, "drawScale", 1.0f);
        }
    }
}