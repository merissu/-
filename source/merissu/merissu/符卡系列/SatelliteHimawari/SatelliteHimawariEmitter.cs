using UnityEngine;
using Verse;

namespace merissu
{
    public class SatelliteHimawariEmitter : Thing
    {
        public Pawn caster;

        private const float Radius = 2.5f;
        private const int SpawnInterval = 2;
        private const int DurationTicks = 600; 

        private int age = 0;
        private float angle = 0f;
        private float visualRotation = 180f;

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead || age >= DurationTicks)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (Find.TickManager.TicksGame % SpawnInterval == 0)
            {
                SpawnAtAngle("SatelliteThing", angle, visualRotation, true);
                SpawnAtAngle("SatelliteThing_New", angle + 180f, visualRotation + 180f, true);

                angle -= 3f;
                visualRotation += 3f;
            }
        }

        private void SpawnAtAngle(string defName, float ang, float rot, bool shouldDrawLightSphere)
        {
            float rad = ang * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(rad),
                0f,
                Mathf.Cos(rad)
            ) * Radius;
            
            SatelliteThing thing = (SatelliteThing)GenSpawn.Spawn(
                ThingDef.Named(defName),
                (caster.DrawPos + offset).ToIntVec3(), 
                caster.Map
            );

            if (thing != null)
            {
                thing.caster = caster;
                thing.fixedOffset = offset;
                thing.finalRotation = rot;
                thing.drawLightSphere = shouldDrawLightSphere; 
            }
        }
    }
}