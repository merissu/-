using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class Thing_XWaveEffect : Thing
    {
        public Pawn caster;
        private int age;

        private const int LifeTimeTicks = 90;
        private const int TicksPerFrame = 5;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                SpawnRing();
                SpawnLasers();
            }
        }

        private void SpawnLasers()
        {
            int count = 6;
            for (int i = 0; i < count; i++)
            {
                Thing t = ThingMaker.MakeThing(ThingDef.Named("XWaveLaser"));
                if (t is Thing_XWaveLaser laser)
                {
                    laser.caster = caster;
                    GenSpawn.Spawn(laser, caster.Position, caster.Map);
                }
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                Destroy();
                return;
            }
            Position = caster.Position;

            int interval = 15;
            int maxAge = 80;

            if (age % interval == 0 && age <= maxAge)
            {
                SpawnRing();
            }

            if (age % 5 == 0 && caster.Map != null)
            {
                float radius = 12f;
                IEnumerable<Thing> targets = GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, radius, true);
                foreach (Thing t in targets)
                {
                    if (t is Pawn p && p != caster && !p.Dead && p.Faction != null && p.Faction.HostileTo(caster.Faction))
                    {
                        HealthUtility.AdjustSeverity(p, HediffDef.Named("XWave"), 1.0f);
                    }
                }
            }

            if (age >= LifeTimeTicks)
            {
                Destroy();
            }
        }

        private void SpawnRing()
        {
            if (caster == null || !caster.Spawned) return;

            Thing ring = ThingMaker.MakeThing(ThingDef.Named("XWaveRing"));
            if (ring is Thing_XWaveRing xr)
            {
                xr.caster = caster;
                GenSpawn.Spawn(xr, caster.Position, caster.Map);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            Vector3 pos = caster.DrawPos;
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            int frame = (age / TicksPerFrame) % 2;
            string texPath = frame == 0
                ? "Other/Wave/bulletFa000"
                : "Other/Wave/bulletFa001";

            Material mat = MaterialPool.MatFrom(
                texPath,
                ShaderDatabase.MoteGlow
            );

            float size = 25f;
            float randomAngle = Rand.Range(0f, 360f);
            Quaternion rot = Quaternion.Euler(0f, randomAngle, 0f);

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                rot,
                new Vector3(size, 1f, size)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}