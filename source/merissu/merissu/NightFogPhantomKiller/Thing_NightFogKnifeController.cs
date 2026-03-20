using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;

namespace merissu
{
    public class Thing_NightFogKnifeController : Thing
    {
        public Pawn caster;
        public Pawn target;

        private int lifeTicks;
        private const int MaxLifeTicks = 600;

        private const int BladeCount = 12;
        private const float OrbitRadius = 1.8f;

        private List<Thing_NightFogKnifeBlade> blades =
            new List<Thing_NightFogKnifeBlade>();

        private float orbitAngle;

        private int fireTick;
        private int fireIndex;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            for (int i = 0; i < BladeCount; i++)
            {
                Thing bladeThing = GenSpawn.Spawn(
                    ThingDef.Named("NightFogKnifeBlade"),
                    Position,
                    map
                );

                Thing_NightFogKnifeBlade blade =
                    bladeThing as Thing_NightFogKnifeBlade;

                blade.controller = this;
                blade.index = i;

                blades.Add(blade);
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || !caster.Spawned || caster.Dead ||
                target == null || !target.Spawned || target.Dead)
            {
                Destroy();
                return;
            }

            lifeTicks++;
            if (lifeTicks >= MaxLifeTicks)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            orbitAngle += 8f;

            for (int i = 0; i < blades.Count; i++)
            {
                float angleStep = 360f / blades.Count;
                float angle = orbitAngle + i * angleStep;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Sin(rad),
                    0f,
                    Mathf.Cos(rad)
                ) * OrbitRadius;

                blades[i].fixedOffset = offset;
            }

            fireTick++;
            if (fireTick >= 1)
            {
                fireTick = 0;
                FireOne();
            }
        }

        private void FireOne()
        {
            if (blades.Count == 0) return;

            ThingDef knifeDef = ThingDef.Named("knife");
            if (knifeDef == null) return;

            Thing_NightFogKnifeBlade blade = blades[fireIndex];

            Vector3 start = caster.DrawPos + blade.fixedOffset;
            Vector3 targetPos = target.DrawPos;

            Projectile proj = (Projectile)GenSpawn.Spawn(
                knifeDef,
                start.ToIntVec3(),
                Map
            );

            proj.Launch(
                caster,
                start,
                targetPos.ToIntVec3(),
                targetPos.ToIntVec3(),
                ProjectileHitFlags.IntendedTarget
            );

            SoundDef.Named("knife")?.PlayOneShot(
                new TargetInfo(caster.Position, Map)
            );

            fireIndex++;
            if (fireIndex >= blades.Count)
                fireIndex = 0;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            for (int i = 0; i < blades.Count; i++)
            {
                if (blades[i] != null && !blades[i].Destroyed)
                    blades[i].Destroy();
            }

            base.Destroy(mode);
        }
    }
}
