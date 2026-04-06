using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_StarlightOrbRed_Follower : Thing
    {
        private Pawn owner;
        private float currentAngle;
        private int lastShotTick = -999;

        private Pawn cachedTarget;
        private int lastScanTick = -999;
        private const int ScanInterval = 30; 

        private const float FollowRadius = 1.5f;
        private const float CombatRadius = 2.5f;
        private const float MinAttackRange = 3f;
        private const float MaxAttackRange = 40f;
        private const int ShootIntervalTicks = 60;

        private static readonly HediffDef SpiritualPowerDef = HediffDef.Named("spiritualpower");
        private static readonly ThingDef ProjectileDef = ThingDef.Named("Missile");
        private const float MinPowerToFire = 0.02f;
        private const float PowerCostPerShot = 0.01f;

        public void Init(Pawn owner)
        {
            this.owner = owner;
        }

        private float GetCurrentSpiritualPower()
        {
            if (owner?.health?.hediffSet == null) return 0f;
            return owner.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef)?.Severity ?? 0f;
        }

        protected override void Tick()
        {
            if (owner == null || owner.Dead || !owner.Spawned)
            {
                if (!Destroyed) this.Destroy();
                return;
            }

            if (this.Position != owner.Position) this.Position = owner.Position;

            if (owner.Drafted && GetCurrentSpiritualPower() >= MinPowerToFire)
            {
                if (Find.TickManager.TicksGame >= lastScanTick + ScanInterval)
                {
                    cachedTarget = FindBestTarget();
                    lastScanTick = Find.TickManager.TicksGame;
                }
            }
            else
            {
                cachedTarget = null;
            }

            if (cachedTarget != null)
            {
                Vector3 aimDir = (cachedTarget.DrawPos - owner.DrawPos).normalized;
                float targetAngle = Mathf.Atan2(aimDir.z, aimDir.x) * Mathf.Rad2Deg;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.15f);

                if (Find.TickManager.TicksGame >= lastShotTick + ShootIntervalTicks)
                {
                    ShootProjectile(cachedTarget);
                    lastShotTick = Find.TickManager.TicksGame;
                }
            }
            else
            {
                float rimworldBackAngle = owner.Rotation.AsAngle + 180f;
                float targetAngle = 90f - rimworldBackAngle;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.05f);
            }
        }

        private Pawn FindBestTarget()
        {
            if (owner.Map == null) return null;

            float minDistSq = MaxAttackRange * MaxAttackRange;
            float minRangeSq = MinAttackRange * MinAttackRange;
            Pawn bestPawn = null;

            IEnumerable<Thing> nearby = GenRadial.RadialDistinctThingsAround(owner.Position, owner.Map, MaxAttackRange, true);
            foreach (Thing t in nearby)
            {
                if (t is Pawn p && p.HostileTo(owner) && !p.Dead && !p.Downed && !p.IsPrisoner)
                {
                    float distSq = p.Position.DistanceToSquared(owner.Position);
                    if (distSq <= minDistSq && distSq >= minRangeSq)
                    {
                        if (GenSight.LineOfSight(owner.Position, p.Position, owner.Map))
                        {
                            bestPawn = p;
                            minDistSq = distSq; 
                        }
                    }
                }
            }
            return bestPawn;
        }

        private void ShootProjectile(Pawn target)
        {
            if (ProjectileDef == null) return;

            Projectile projectile = (Projectile)GenSpawn.Spawn(ProjectileDef, DrawPos.ToIntVec3(), owner.Map);
            projectile.Launch(owner, DrawPos, target, target, ProjectileHitFlags.All, false, null);

            ConsumeSpiritualPower(PowerCostPerShot);
        }

        private void ConsumeSpiritualPower(float amount)
        {
            Hediff hediff = owner.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef);
            if (hediff != null) hediff.Severity -= amount;
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (owner == null) return base.DrawPos;

                float rad = currentAngle * Mathf.Deg2Rad;
                float dist = owner.Drafted ? CombatRadius : FollowRadius;
                float xOffset = Mathf.Cos(rad) * dist;
                float zOffset = Mathf.Sin(rad) * dist;
                float floatingY = Mathf.Sin(Find.TickManager.TicksGame * 0.05f) * 0.15f;

                Vector3 pos = owner.DrawPos + new Vector3(xOffset, 0.5f, zOffset + floatingY);
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                return pos;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_References.Look(ref cachedTarget, "cachedTarget");
            Scribe_Values.Look(ref currentAngle, "currentAngle");
            Scribe_Values.Look(ref lastShotTick, "lastShotTick");
        }
    }
}