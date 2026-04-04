using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class Projectile_HomingTalisman : Projectile
    {
        private int circleTicks = 0;
        private float angle = 0f;
        private const int MaxCircleTicks = 300;
        private const float CircleRadius = 1.5f;

        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero; 

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false, Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            this.currentRealPos = origin;
            this.currentVelocity = (intendedTarget.CenterVector3 - origin).normalized;
        }

        private int scanCooldown = 0;

        protected override void Tick()
        {
            if (currentRealPos == Vector3.zero) currentRealPos = this.DrawPos;

            Pawn targetPawn = this.intendedTarget.Thing as Pawn;

            if (targetPawn == null || targetPawn.Dead || targetPawn.Destroyed || !targetPawn.Spawned)
            {
                scanCooldown--;
                if (scanCooldown <= 0)
                {
                    targetPawn = FindNearestEnemy();
                    if (targetPawn != null) this.intendedTarget = new LocalTargetInfo(targetPawn);
                    scanCooldown = 30;
                }
            }

            Vector3 targetVector;
            float turnRate; 

            if (targetPawn != null)
            {
                circleTicks = 0;
                targetVector = targetPawn.DrawPos;
                turnRate = 0.07f; 
            }
            else
            {
                circleTicks++;
                if (circleTicks >= MaxCircleTicks) { this.Destroy(); return; }
                angle += 0.1f;
                targetVector = currentRealPos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * CircleRadius;
                turnRate = 0.03f; 
            }

            Vector3 desiredDir = (targetVector - currentRealPos).normalized;
            currentVelocity = Vector3.Slerp(currentVelocity, desiredDir, turnRate).normalized;

            float step = this.def.projectile.speed / 100f;
            currentRealPos += currentVelocity * step;

            this.Position = currentRealPos.ToIntVec3();

            if (targetPawn != null)
            {
                float contactDist = (targetPawn.RaceProps.baseBodySize * 0.5f) + 0.3f;

                if (Vector3.Distance(currentRealPos, targetPawn.DrawPos) < contactDist)
                {
                    this.Impact(targetPawn);
                    return;
                }
            }
        }

        public override Vector3 DrawPos => currentRealPos;

        public override Quaternion ExactRotation => (currentVelocity == Vector3.zero) ? base.ExactRotation : Quaternion.LookRotation(currentVelocity);

        private Pawn FindNearestEnemy()
        {
            if (this.Map == null) return null;
            return (Pawn)GenClosest.ClosestThingReachable(
                this.Position,
                this.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                Verse.AI.PathEndMode.Touch,
                TraverseParms.For(TraverseMode.ByPawn),
                999f,
                x => x is Pawn p && !p.Dead && p.HostileTo(this.launcher));
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (hitThing == null && FindNearestEnemy() != null) return;
            if (hitThing != null)
            {
                float damageAmount = this.def.projectile.GetDamageAmount(this.launcher, null);
                float armorPenetration = this.def.projectile.GetArmorPenetration(this.launcher, null);
                DamageDef damageDef = this.def.projectile.damageDef ?? DamageDefOf.Bullet;
                DamageInfo dinfo = new DamageInfo(damageDef, damageAmount, armorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo);
            }
            base.Impact(hitThing, blockedByShield);
        }
    }
}