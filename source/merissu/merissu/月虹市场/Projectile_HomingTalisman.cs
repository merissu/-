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

        private float currentTurnRate = 0.03f;
        private const float TurnRateAcceleration = 0.001f; 

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false, Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            this.currentRealPos = origin;
            this.currentVelocity = (intendedTarget.CenterVector3 - origin).normalized;
            this.currentTurnRate = 0.03f; 
        }

        private int scanCooldown = 0;

        protected override void Tick()
        {
            if (currentRealPos == Vector3.zero) currentRealPos = this.DrawPos;

            Thing targetThing = this.intendedTarget.Thing;

            bool isTargetInvalid = targetThing == null || targetThing.Destroyed || (targetThing is Pawn p && p.Dead);

            if (isTargetInvalid)
            {
                scanCooldown--;
                if (scanCooldown <= 0)
                {
                    targetThing = FindNearestEnemyThing();
                    if (targetThing != null) this.intendedTarget = new LocalTargetInfo(targetThing);
                    scanCooldown = 30;
                    currentTurnRate = 0.03f; 
                }
            }

            Vector3 targetVector;

            if (targetThing != null && !isTargetInvalid)
            {
                circleTicks = 0;
                targetVector = targetThing.DrawPos;

                currentTurnRate += TurnRateAcceleration;
                if (currentTurnRate > 1f) currentTurnRate = 1f;
            }
            else
            {
                circleTicks++;
                if (circleTicks >= MaxCircleTicks) { this.Destroy(); return; }
                angle += 0.1f;
                targetVector = currentRealPos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * CircleRadius;
                currentTurnRate = 0.03f; 
            }

            Vector3 desiredDir = (targetVector - currentRealPos).normalized;

            currentVelocity = Vector3.Slerp(currentVelocity, desiredDir, currentTurnRate).normalized;

            float step = this.def.projectile.speed / 100f;
            currentRealPos += currentVelocity * step;

            this.Position = currentRealPos.ToIntVec3();

            if (targetThing != null && !isTargetInvalid)
            {
                float contactDist = 0.3f;
                if (targetThing is Pawn targetPawn)
                {
                    contactDist += targetPawn.RaceProps.baseBodySize * 0.5f;
                }
                else if (targetThing.def != null)
                {
                    contactDist += Mathf.Max(targetThing.def.size.x, targetThing.def.size.z) * 0.5f;
                }

                if (Vector3.Distance(currentRealPos, targetThing.DrawPos) < contactDist)
                {
                    this.Impact(targetThing);
                    return;
                }
            }
        }

        public override Vector3 DrawPos => currentRealPos;

        public override Quaternion ExactRotation => (currentVelocity == Vector3.zero) ? base.ExactRotation : Quaternion.LookRotation(currentVelocity);

        private Thing FindNearestEnemyThing()
        {
            if (this.Map == null) return null;
            return GenClosest.ClosestThingReachable(
                this.Position,
                this.Map,
                ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), 
                Verse.AI.PathEndMode.Touch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                999f,
                x => {
                    if (x.Destroyed) return false;
                    if (x is Pawn p && p.Dead) return false;
                    if (x.Faction != null && this.launcher != null && this.launcher.Faction != null)
                    {
                        return x.Faction.HostileTo(this.launcher.Faction);
                    }
                    return false;
                });
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (hitThing == null && FindNearestEnemyThing() != null) return;
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