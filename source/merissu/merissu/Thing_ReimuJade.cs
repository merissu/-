using RimWorld;
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

            orbitAngle += OrbitSpeed;
            selfRotationAngle += SelfRotateSpeed;
            if (orbitAngle > 360f) orbitAngle -= 360f;
            if (selfRotationAngle > 360f) selfRotationAngle -= 360f;

            if (Find.TickManager.TicksGame >= nextFireTick)
            {
                if (TryFireAtTarget())
                {
                    nextFireTick = Find.TickManager.TicksGame + FireIntervalTicks;
                }
                else
                {
                    nextFireTick = Find.TickManager.TicksGame + 10;
                }
            }
        }

        private bool TryFireAtTarget()
        {
            if (caster.Map == null) return false;

            Pawn target = null;
            float minDist = ScanRadius;

            var allPawns = caster.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];

                if (p != null &&
                    !p.Dead &&
                    p.Spawned &&
                    p.Faction != null &&
                    p.Faction.HostileTo(caster.Faction))
                {
                    float dist = p.Position.DistanceTo(caster.Position);
                    if (dist <= minDist)
                    {
                        minDist = dist;
                        target = p;
                    }
                }
            }

            if (target == null) return false;

            FireBullet(target);
            return true;
        }
        private void FireBullet(Pawn target)
        {
            if (BulletDef == null) return;

            Vector3 origin = GetCurrentDrawPos();

            Projectile projectile = (Projectile)GenSpawn.Spawn(BulletDef, origin.ToIntVec3(), caster.Map);

            projectile.Launch(caster, origin, target, target, ProjectileHitFlags.All);

            ShootSound?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
        }
        private Vector3 GetCurrentDrawPos()
        {
            Vector3 center = caster.DrawPos;
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * Radius, 0f, Mathf.Sin(rad) * Radius);
            Vector3 pos = center + offset;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return pos;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 finalPos = GetCurrentDrawPos();
            float sizeScale = 0.5f;
            Vector3 scaleVector = new Vector3(sizeScale, 1f, sizeScale);

            Matrix4x4 matrix = default;
            matrix.SetTRS(finalPos, Quaternion.AngleAxis(selfRotationAngle, Vector3.up), scaleVector);

            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref orbitAngle, "orbitAngle");
            Scribe_Values.Look(ref selfRotationAngle, "selfRotationAngle");
            Scribe_Values.Look(ref nextFireTick, "nextFireTick");
        }
    }
}