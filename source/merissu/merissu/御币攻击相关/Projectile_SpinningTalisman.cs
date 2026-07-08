using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class SpinningTalismanGraphics
    {
        public static readonly Material TrailMat = MaterialPool.MatFrom("Projectiles/REIMU/Talisman/bulletB001", ShaderDatabase.MoteGlow);
    }

    public class Projectile_SpinningTalisman : Projectile
    {
        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero;
        private float currentTurnRate = 0.03f;
        private const float TurnRateAcceleration = 0.001f;

        private int circleTicks = 0;
        private float circleAngle = 0f;
        private const int MaxCircleTicks = 300;
        private const float CircleRadius = 1.5f;

        private int scanCooldown = 0;
        private const int ScanInterval = 30;

        private float spinAngle = 0f;
        private const float SpinSpeed = 20f; 

        private bool isDecelerating = false;
        private int decelerateTicks = 0;
        private const int MaxDecelerateTicks = 90;       
        private const int FadeOutDuration = 30;           
        private const float SlowSpeedMultiplier = 0.05f;  
        private const int PenetrateDamageInterval = 2;
        private int nextDamageTick = 0;

        private int trailSpawnCD = 0;
        private const int TrailInterval = 3;

        public override Vector3 DrawPos => currentRealPos;

        public override Quaternion ExactRotation
        {
            get
            {
                Vector3 flatVel = currentVelocity;
                flatVel.y = 0;
                if (flatVel.sqrMagnitude > 0.0001f)
                {
                    Quaternion baseRot = Quaternion.LookRotation(flatVel, Vector3.up);
                    return baseRot * Quaternion.AngleAxis(spinAngle, Vector3.up);
                }
                return Quaternion.AngleAxis(spinAngle, Vector3.up);
            }
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false, Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            currentRealPos = origin;
            currentVelocity = (intendedTarget.CenterVector3 - origin).normalized;
            currentTurnRate = 0.03f;
            spinAngle = 0f;
            isDecelerating = false;
            decelerateTicks = 0;
            nextDamageTick = 0;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (isDecelerating && decelerateTicks >= MaxDecelerateTicks - FadeOutDuration)
            {
                int fadeTicks = decelerateTicks - (MaxDecelerateTicks - FadeOutDuration);
                float alpha = 1f - (float)fadeTicks / FadeOutDuration;
                alpha = Mathf.Clamp01(alpha);

                Material fadedMat = FadedMaterialPool.FadedVersionOf(
                    def.graphicData.Graphic.MatSingle, alpha);

                Vector3 drawPos = drawLoc;
                drawPos.y = def.altitudeLayer.AltitudeFor();

                Vector3 drawSize = def.graphicData.drawSize;
                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, ExactRotation,
                    new Vector3(drawSize.x, 1f, drawSize.y));

                Graphics.DrawMesh(MeshPool.plane10, matrix, fadedMat, 0);
            }
            else
            {
                base.DrawAt(drawLoc, flip);
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (currentRealPos == Vector3.zero)
                currentRealPos = DrawPos;

            if (isDecelerating)
            {
                DeceleratingTick();
                return;
            }

            Thing targetThing = intendedTarget.Thing;
            bool targetInvalid = targetThing == null || targetThing.Destroyed || (targetThing is Pawn p && p.Dead);

            if (targetInvalid)
            {
                scanCooldown--;
                if (scanCooldown <= 0)
                {
                    targetThing = FindNearestEnemyThing();
                    if (targetThing != null)
                        intendedTarget = new LocalTargetInfo(targetThing);
                    scanCooldown = ScanInterval;
                    currentTurnRate = 0.03f;
                }
            }

            Vector3 targetPos;
            if (targetThing != null && !targetInvalid)
            {
                circleTicks = 0;
                targetPos = targetThing.DrawPos;
                currentTurnRate += TurnRateAcceleration;
                if (currentTurnRate > 1f) currentTurnRate = 1f;
            }
            else
            {
                circleTicks++;
                if (circleTicks >= MaxCircleTicks)
                {
                    Destroy();
                    return;
                }
                circleAngle += 0.1f;
                targetPos = currentRealPos + new Vector3(Mathf.Cos(circleAngle), 0, Mathf.Sin(circleAngle)) * CircleRadius;
                currentTurnRate = 0.03f;
            }

            Vector3 desiredDir = (targetPos - currentRealPos).normalized;
            currentVelocity = Vector3.Slerp(currentVelocity, desiredDir, currentTurnRate).normalized;
            float step = def.projectile.speed / 100f;
            currentRealPos += currentVelocity * step;
            Position = currentRealPos.ToIntVec3();

            spinAngle += SpinSpeed;
            SpawnTrailMote();

            if (targetThing != null && !targetInvalid)
            {
                float contactDist = 0.3f;
                if (targetThing is Pawn targetPawn)
                    contactDist += Mathf.Min(targetPawn.RaceProps.baseBodySize * 0.15f, 0.4f);
                else if (targetThing.def != null)
                    contactDist += Mathf.Max(targetThing.def.size.x, targetThing.def.size.z) * 0.5f;

                if (Vector3.Distance(currentRealPos, targetThing.DrawPos) < contactDist)
                {
                    DealDamageTo(targetThing);
                    EnterDecelerationMode();
                }
            }
        }

        private void EnterDecelerationMode()
        {
            isDecelerating = true;
            decelerateTicks = 0;
            nextDamageTick = 0;
        }

        private void DeceleratingTick()
        {
            float step = def.projectile.speed * SlowSpeedMultiplier / 100f;
            currentRealPos += currentVelocity.normalized * step;
            Position = currentRealPos.ToIntVec3();

            spinAngle += SpinSpeed;
            SpawnTrailMote();

            if (nextDamageTick <= 0)
            {
                Thing victim = GetAttackTargetInCell();
                if (victim != null)
                    DealDamageTo(victim);
                nextDamageTick = PenetrateDamageInterval;
            }
            else
            {
                nextDamageTick--;
            }

            decelerateTicks++;
            if (decelerateTicks >= MaxDecelerateTicks || !currentRealPos.InBounds(Map))
                Destroy();
        }

        private Thing GetAttackTargetInCell()
        {
            if (Map == null) return null;
            foreach (Thing t in Map.thingGrid.ThingsAt(Position))
            {
                if (t is Pawn p && !p.Dead && p.Faction != null && launcher != null && launcher.Faction != null
                    && p.Faction.HostileTo(launcher.Faction))
                    return p;
            }
            return null;
        }

        private void DealDamageTo(Thing target)
        {
            if (target == null) return;
            float dmg = def.projectile.GetDamageAmount(launcher, null);
            float pen = def.projectile.GetArmorPenetration(launcher, null);
            DamageDef dd = def.projectile.damageDef ?? DamageDefOf.Bullet;
            DamageInfo dinfo = new DamageInfo(dd, dmg, pen, ExactRotation.eulerAngles.y, launcher, null, equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
            target.TakeDamage(dinfo);
        }

        private void SpawnTrailMote()
        {
            trailSpawnCD--;
            if (trailSpawnCD > 0) return;
            trailSpawnCD = TrailInterval;

            if (Map == null) return;

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamed("Mote_REIMU_TalismanTrail");
            Mote_REIMU_TalismanTrail trail = (Mote_REIMU_TalismanTrail)ThingMaker.MakeThing(moteDef);
            trail.exactPosition = currentRealPos;
            trail.initialRotation = spinAngle;
            GenSpawn.Spawn(trail, Position, Map);
        }

        private Thing FindNearestEnemyThing()
        {
            if (Map == null) return null;
            return GenClosest.ClosestThingReachable(
                Position,
                Map,
                ThingRequest.ForGroup(ThingRequestGroup.AttackTarget),
                Verse.AI.PathEndMode.Touch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors),
                999f,
                x =>
                {
                    if (x.Destroyed) return false;
                    if (x is Pawn p && p.Dead) return false;
                    if (x.Faction != null && launcher != null && launcher.Faction != null)
                        return x.Faction.HostileTo(launcher.Faction);
                    return false;
                });
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
        }
    }
    public class Mote_REIMU_TalismanTrail : Thing
    {
        public Vector3 exactPosition;
        public float initialRotation;
        private int age = 0;
        private const int MaxAge = 15;
        private const float StartScale = 1f;
        private const float EndScale = 3f;
        private const float SpinSpeed = 10f; 

        public override Vector3 DrawPos => exactPosition;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = age / (float)MaxAge;
            float scale = Mathf.Lerp(StartScale, EndScale, progress);
            float alpha = 1f - progress;

            drawLoc = exactPosition;
            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(SpinningTalismanGraphics.TrailMat, alpha);
            float currentAngle = initialRotation + age * SpinSpeed;
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, rot, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}