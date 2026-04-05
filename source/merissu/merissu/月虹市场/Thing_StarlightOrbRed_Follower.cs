using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace merissu
{
    public class Thing_StarlightOrbRed_Follower : Thing
    {
        private Pawn owner;
        private float currentAngle;
        private int lastShotTick = -999;

        private const float FollowRadius = 1.5f;
        private const float CombatRadius = 2.5f;
        private const float MinAttackRange = 3f;  
        private const float MaxAttackRange = 40f; 
        private const int ShootIntervalTicks = 60; 

        public void Init(Pawn owner)
        {
            this.owner = owner;
        }

        protected override void Tick()
        {
            if (owner == null || owner.Dead || !owner.Spawned)
            {
                this.Destroy();
                return;
            }

            Pawn target = null;
            if (owner.Drafted)
            {
                target = FindBestTarget();
            }

            if (target != null)
            {
                Vector3 aimDir = (target.DrawPos - owner.DrawPos).normalized;
                float targetAngle = Mathf.Atan2(aimDir.z, aimDir.x) * Mathf.Rad2Deg;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.15f);

                if (Find.TickManager.TicksGame >= lastShotTick + ShootIntervalTicks)
                {
                    ShootProjectile(target);
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
            return (Pawn)GenClosest.ClosestThingReachable(
                owner.Position,
                owner.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                Verse.AI.PathEndMode.OnCell,
                TraverseParms.For(owner),
                MaxAttackRange, 
                x =>
                {
                    Pawn p = x as Pawn;
                    if (p == null || p.Dead || p.Downed || !p.HostileTo(owner) || p.IsPrisoner)
                        return false;

                    float dist = p.Position.DistanceTo(owner.Position);
                    return dist >= MinAttackRange && dist <= MaxAttackRange &&
                           GenSight.LineOfSight(owner.Position, p.Position, owner.Map);
                });
        }

        private void ShootProjectile(Pawn target)
        {
            ThingDef projectileDef = ThingDef.Named("Missile");
            if (projectileDef == null) return;

            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, DrawPos.ToIntVec3(), Map);

            projectile.Launch(owner, DrawPos, target, target, ProjectileHitFlags.All, false, null);

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

                return owner.DrawPos + new Vector3(xOffset, 0.5f, zOffset + floatingY);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref currentAngle, "currentAngle");
            Scribe_Values.Look(ref lastShotTick, "lastShotTick");
        }
    }
}