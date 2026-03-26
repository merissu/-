using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class MerissuFleckDefOf
    {
        public static FleckDef merissu_HailStone_Fall;
        public static FleckDef merissu_HailStone_Bounce;

        static MerissuFleckDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MerissuFleckDefOf));
        }
    }

    public class WeatherEvent_HailShower : WeatherEvent
    {
        private struct PendingImpact
        {
            public int ticksLeft;
            public Vector3 impactPos;
            public float scale;
        }

        private readonly List<PendingImpact> pending = new List<PendingImpact>();

        private const int SpawnIntervalTicks = 3;   
        private const int MaxPendingImpacts = 280;  

        private int ageTicks;
        private readonly int durationTicks;

        public override bool Expired => ageTicks >= durationTicks && pending.Count == 0;

        public WeatherEvent_HailShower(Map map) : base(map)
        {
            durationTicks = Rand.RangeInclusive(320, 520); 
        }

        public override void FireEvent() { }

        public override void WeatherEventTick()
        {
            ageTicks++;

            if (map != Find.CurrentMap)
            {
                pending.Clear();
                return;
            }

            if (ageTicks <= durationTicks && ageTicks % SpawnIntervalTicks == 0)
            {
                SpawnFallBatchInView();
            }

            TickImpacts();
        }

        private void SpawnFallBatchInView()
        {
            if (MerissuFleckDefOf.merissu_HailStone_Fall == null) return;

            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(6).ClipInsideMap(map);

            int spawnCount = Mathf.Clamp(view.Area / 1400, 1, 3);

            for (int i = 0; i < spawnCount; i++)
            {
                IntVec3 c = view.RandomCell;
                Vector3 p = c.ToVector3Shifted();
                p.x += Rand.Range(-0.35f, 0.35f);
                p.z += Rand.Range(-0.35f, 0.35f);

                if (!p.ShouldSpawnMotesAt(map, drawOffscreen: false)) continue;

                float angle = Rand.Range(172f, 188f);
                float speed = Rand.Range(13.5f, 19f);
                float airTime = Rand.Range(0.10f, 0.15f);
                float scale = Rand.Range(0.26f, 0.40f);

                var d = new FleckCreationData
                {
                    def = MerissuFleckDefOf.merissu_HailStone_Fall,
                    spawnPosition = p,
                    scale = scale,
                    velocityAngle = angle,
                    velocitySpeed = speed,
                    rotationRate = Rand.Range(-420f, 420f),
                    airTimeLeft = airTime,
                    exactScale = new Vector3(scale, 1f, scale),
                    ageTicksOverride = -1
                };
                map.flecks.CreateFleck(d);

                if (pending.Count < MaxPendingImpacts)
                {
                    Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    Vector3 impact = p + dir * (speed * airTime);

                    pending.Add(new PendingImpact
                    {
                        ticksLeft = Mathf.Max(1, Mathf.CeilToInt(airTime * 60f)),
                        impactPos = impact,
                        scale = scale
                    });
                }
            }
        }

        private void TickImpacts()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                PendingImpact x = pending[i];
                x.ticksLeft--;

                if (x.ticksLeft <= 0)
                {
                    SpawnBounce(x.impactPos, x.scale);
                    pending.RemoveAt(i);
                }
                else
                {
                    pending[i] = x;
                }
            }
        }

        private void SpawnBounce(Vector3 pos, float srcScale)
        {
            if (MerissuFleckDefOf.merissu_HailStone_Bounce == null) return;
            if (!pos.InBounds(map) || !pos.ShouldSpawnMotesAt(map, drawOffscreen: false)) return;

            float bounceAngle = Rand.Range(262f, 278f);
            float bounceScale = srcScale * Rand.Range(0.88f, 0.96f);

            var b = new FleckCreationData
            {
                def = MerissuFleckDefOf.merissu_HailStone_Bounce,
                spawnPosition = pos,
                scale = bounceScale,
                velocityAngle = bounceAngle,
                velocitySpeed = Rand.Range(1.0f, 1.8f),
                rotationRate = Rand.Range(-260f, 260f),
                airTimeLeft = Rand.Range(0.26f, 0.36f),
                exactScale = new Vector3(bounceScale, 1f, bounceScale),
                ageTicksOverride = -1
            };
            map.flecks.CreateFleck(b);
        }
    }
}