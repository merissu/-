using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class FireballGraphics
    {
        public static readonly Material[] FireballFrames = new Material[11];
        public static readonly Material ShockwaveMat;
        public static readonly Material CenterMat;
        static FireballGraphics()
        {
            for (int i = 0; i < 11; i++)
            {
                FireballFrames[i] = MaterialPool.MatFrom($"Projectiles/fireball/BulletBa{i:D3}", ShaderDatabase.MoteGlow);
            }
            ShockwaveMat = MaterialPool.MatFrom("Projectiles/fireball/bulletLb004", ShaderDatabase.MoteGlow);
            CenterMat = MaterialPool.MatFrom("Projectiles/fireball/BulletBb001", ShaderDatabase.MoteGlow);
        }
    }

    public class Projectile_Fireball_Custom : Projectile
    {
        private int ticks = 0;
        private const int TicksPerFrame = 2;
        private const float DrawScale = 1.5f;

        private int circleTicks = 0;
        private float angle = 0f;
        private const int MaxCircleTicks = 300;
        private const float CircleRadius = 1.5f;

        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero;

        private float currentTurnRate = 0.03f;
        private const float TurnRateAcceleration = 0.001f;
        private int scanCooldown = 0;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false, Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            this.currentRealPos = origin;
            this.currentVelocity = (intendedTarget.CenterVector3 - origin).normalized;
            this.currentTurnRate = 0.03f;
        }

        protected override void Tick()
        {
            base.Tick();
            ticks++; 

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

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = (ticks / TicksPerFrame) % 11;
            Material mat = FireballGraphics.FireballFrames[frame];

            drawLoc.y = AltitudeLayer.Projectile.AltitudeFor();

            float rotAngle = ExactRotation.eulerAngles.y;
            Quaternion rot = Quaternion.AngleAxis(rotAngle, Vector3.up);

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc,
                rot,
                new Vector3(DrawScale, 1f, DrawScale)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

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

            Map map = base.Map;
            Vector3 pos = this.DrawPos;

            if (hitThing != null)
            {
                float damageAmount = this.def.projectile.GetDamageAmount(this.launcher, null);
                float armorPenetration = this.def.projectile.GetArmorPenetration(this.launcher, null);
                DamageDef damageDef = this.def.projectile.damageDef ?? DamageDefOf.Flame; 
                DamageInfo dinfo = new DamageInfo(damageDef, damageAmount, armorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo);
            }

            base.Impact(hitThing, blockedByShield);

            if (map != null)
            {
                Thing shockwave = ThingMaker.MakeThing(ThingDef.Named("Fireball_Shockwave"));
                GenSpawn.Spawn(shockwave, pos.ToIntVec3(), map);
                if (shockwave is Thing_FireballShockwave sw)
                {
                    sw.exactPosition = pos;
                }
            }
        }
    }
    public class Thing_FireballShockwave : Thing
    {
        private int age = 0;
        private const int MaxAge = 12; 
        public Vector3 exactPosition;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (exactPosition == Vector3.zero) exactPosition = this.Position.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;

            float scale = Mathf.Lerp(1f, 5f, progress);
            float alpha = Mathf.Lerp(1f, 1f, 0f) * (1f - progress); 

            Material shockMat = FadedMaterialPool.FadedVersionOf(
                FireballGraphics.ShockwaveMat,
                alpha
            );

            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 shockMatrix = Matrix4x4.TRS(
                drawLoc,
                Quaternion.identity,
                new Vector3(scale, 1f, scale)
            );

            Graphics.DrawMesh(MeshPool.plane10, shockMatrix, shockMat, 0);

            Material centerMat = FadedMaterialPool.FadedVersionOf(
                FireballGraphics.CenterMat,
                alpha
            );

            Matrix4x4 centerMatrix = Matrix4x4.TRS(
                drawLoc,
                Quaternion.identity,
                Vector3.one 
            );

            Graphics.DrawMesh(MeshPool.plane10, centerMatrix, centerMat, 0);
        }
    }
}