using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class Thing_SagesStone : Thing
    {
        private Pawn caster;

        private float angle;
        private const float Radius = 1.4f;
        private const float RotateSpeed = 2.5f;

        private const int ScanInterval = 30; 
        private const float ScanRadius = 30f;
        private int scanTickCounter;

        private string projectileDefName;
        private Pawn currentTarget;
        private int shotsLeft;
        private int nextShotTick;

        private const int ShotsPerBurst = 5;
        private const int ShotInterval = 6;


        public void Init(Pawn pawn, int index, int total)
        {
            caster = pawn;
            angle = 360f / total * index;

            if (def.defName.Contains("Fire"))
                projectileDefName = "SagesStoneBullet_Fire";
            else if (def.defName.Contains("Water"))
                projectileDefName = "SagesStoneBullet_Water";
            else if (def.defName.Contains("Wood"))
                projectileDefName = "SagesStoneBullet_Wood";
            else if (def.defName.Contains("Metal"))
                projectileDefName = "SagesStoneBullet_Metal";
            else
                projectileDefName = "SagesStoneBullet_Earth";
        }


        protected override void Tick()
        {
            if (caster == null || !caster.Spawned || caster.Dead)
            {
                if (!Destroyed) Destroy();
                return;
            }

            angle += RotateSpeed;

            if (currentTarget != null && (!currentTarget.Spawned || currentTarget.Dead || currentTarget.Map != caster.Map))
            {
                currentTarget = null;
                shotsLeft = 0;
            }

            if (shotsLeft <= 0)
            {
                scanTickCounter++;
                if (scanTickCounter >= ScanInterval)
                {
                    scanTickCounter = 0;
                    TryAcquireTarget();
                }
            }
            else
            {
                if (currentTarget != null && Find.TickManager.TicksGame >= nextShotTick)
                {
                    FireSingleShot(currentTarget);
                    shotsLeft--;
                    nextShotTick = Find.TickManager.TicksGame + ShotInterval;
                }
            }
        }

        private void TryAcquireTarget()
        {
            currentTarget = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p =>
                    p.Faction != null &&
                    caster.Faction != null &&
                    p.Faction.HostileTo(caster.Faction) &&
                    !p.Dead &&
                    p.Position.DistanceTo(caster.Position) <= ScanRadius)
                .OrderBy(p => p.Position.DistanceTo(caster.Position))
                .FirstOrDefault();

            if (currentTarget != null)
            {
                shotsLeft = ShotsPerBurst;
                nextShotTick = Find.TickManager.TicksGame;
            }
        }

        private void FireSingleShot(Pawn target)
        {
            ThingDef projDef = ThingDef.Named(projectileDefName);
            if (projDef == null) return;

            Vector3 origin = GetCurrentDrawPos();
            origin.y = AltitudeLayer.Projectile.AltitudeFor();

            Projectile proj = (Projectile)GenSpawn.Spawn(projDef, origin.ToIntVec3(), caster.Map);

            proj.Launch(
                caster,
                origin,
                target,
                target,
                ProjectileHitFlags.All
            );
        }


        private Vector3 GetCurrentDrawPos()
        {
            Vector3 center = caster.DrawPos;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * Radius,
                0f,
                Mathf.Sin(rad) * Radius
            );

            Vector3 pos = center + offset;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return pos;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 pos = GetCurrentDrawPos();

            Graphics.DrawMesh(
                MeshPool.plane10,
                pos,
                Quaternion.identity,
                Graphic.MatSingle,
                0
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref shotsLeft, "shotsLeft");
            Scribe_Values.Look(ref nextShotTick, "nextShotTick");
            Scribe_Values.Look(ref projectileDefName, "projectileDefName");
        }
    }
}