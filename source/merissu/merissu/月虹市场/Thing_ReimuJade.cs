using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Thing_ReimuJade : Thing
    {
        private Pawn caster;
        private float orbitAngle;
        private float selfRotationAngle;

        private const float Radius = 1.2f;
        private const float OrbitSpeed = 1.5f;
        private const float SelfRotateSpeed = 8f;

        private const float ScanRadius = 30f;
        private const int FireIntervalTicks = 12;
        private int nextFireTick;

        private Pawn cachedTarget;
        private int lastScanTick = -999;
        private const int ScanInterval = 30; 

        private int shotCount = 0;
        private static readonly HediffDef SpiritualPowerDef = HediffDef.Named("spiritualpower");

        private ThingDef cachedBulletDef;
        private SoundDef cachedSoundDef;

        private ThingDef BulletDef => cachedBulletDef ?? (cachedBulletDef = ThingDef.Named("TrackingHakureiTalisman"));
        private SoundDef ShootSound => cachedSoundDef ?? (cachedSoundDef = SoundDef.Named("Trackinggohei"));

        public void Init(Pawn pawn)
        {
            this.caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                if (!Destroyed) Destroy();
                return;
            }

            orbitAngle = (orbitAngle + OrbitSpeed) % 360f;
            selfRotationAngle = (selfRotationAngle + SelfRotateSpeed) % 360f;

            if (Find.TickManager.TicksGame >= nextFireTick)
            {
                if (GetCurrentSpiritualPower() > 0.02f)
                {
                    Pawn target = GetOrUpdateTarget();
                    if (target != null)
                    {
                        FireBullet(target);
                        nextFireTick = Find.TickManager.TicksGame + FireIntervalTicks;
                    }
                    else
                    {
                        nextFireTick = Find.TickManager.TicksGame + 30;
                    }
                }
                else
                {
                    nextFireTick = Find.TickManager.TicksGame + 60;
                }
            }
        }

        private Pawn GetOrUpdateTarget()
        {
            if (Find.TickManager.TicksGame >= lastScanTick + ScanInterval || cachedTarget == null || cachedTarget.Dead || cachedTarget.Downed || !cachedTarget.Spawned)
            {
                cachedTarget = FindNewTarget();
                lastScanTick = Find.TickManager.TicksGame;
            }
            return cachedTarget;
        }

        private Pawn FindNewTarget()
        {
            if (caster.Map == null) return null;

            Pawn bestTarget = null;
            float minDistSq = ScanRadius * ScanRadius;

            IEnumerable<Thing> nearby = GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, ScanRadius, true);

            foreach (Thing t in nearby)
            {
                if (t is Pawn p && p.HostileTo(caster) && !p.Dead && !p.Downed && !p.IsPrisoner)
                {
                    float distSq = p.Position.DistanceToSquared(caster.Position);
                    if (distSq <= minDistSq)
                    {
                        if (GenSight.LineOfSight(caster.Position, p.Position, caster.Map))
                        {
                            minDistSq = distSq;
                            bestTarget = p;
                        }
                    }
                }
            }
            return bestTarget;
        }

        private float GetCurrentSpiritualPower()
        {
            return caster.health?.hediffSet?.GetFirstHediffOfDef(SpiritualPowerDef)?.Severity ?? 0f;
        }

        private void FireBullet(Pawn target)
        {
            if (BulletDef == null) return;

            Vector3 origin = GetCurrentDrawPos();
            Projectile projectile = (Projectile)GenSpawn.Spawn(BulletDef, origin.ToIntVec3(), caster.Map);
            projectile.Launch(caster, origin, target, target, ProjectileHitFlags.All);
            ShootSound?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));

            shotCount++;
            if (shotCount >= 3)
            {
                ConsumeSpiritualPower(0.01f);
                shotCount = 0;
            }
        }

        private void ConsumeSpiritualPower(float amount)
        {
            Hediff hediff = caster.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef);
            if (hediff != null) hediff.Severity -= amount;
        }

        private Vector3 GetCurrentDrawPos()
        {
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 pos = caster.DrawPos + new Vector3(Mathf.Cos(rad) * Radius, 0f, Mathf.Sin(rad) * Radius);
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return pos;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 finalPos = GetCurrentDrawPos();
            Matrix4x4 matrix = Matrix4x4.TRS(finalPos, Quaternion.AngleAxis(selfRotationAngle, Vector3.up), new Vector3(0.5f, 1f, 0.5f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref orbitAngle, "orbitAngle");
            Scribe_Values.Look(ref selfRotationAngle, "selfRotationAngle");
            Scribe_Values.Look(ref nextFireTick, "nextFireTick");
            Scribe_Values.Look(ref shotCount, "shotCount", 0);
        }
    }
}