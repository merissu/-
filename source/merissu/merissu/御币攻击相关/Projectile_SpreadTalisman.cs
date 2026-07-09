using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class SpreadTalismanGraphics
    {
        public static readonly Material TrailMat = MaterialPool.MatFrom("Projectiles/REIMU/Talisman/bulletBb001", ShaderDatabase.MoteGlow);
    }

    public class Projectile_SpreadTalisman : Projectile
    {
        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 currentVelocity = Vector3.zero;

        private float spinAngle = 0f;
        private const float SpinSpeed = 20f;

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
            spinAngle = 0f;
        }

        protected override void Tick()
        {
            base.Tick();

            if (currentRealPos == Vector3.zero)
                currentRealPos = DrawPos;

            float step = def.projectile.speed / 100f;
            currentRealPos += currentVelocity * step;
            Position = currentRealPos.ToIntVec3();

            spinAngle += SpinSpeed;
            SpawnTrailMote();

            Thing hitThing = CheckCollision();
            if (hitThing != null)
            {
                DealDamageTo(hitThing);
                Destroy();   
                return;
            }

            if (!currentRealPos.InBounds(Map))
                Destroy();
        }

        private Thing CheckCollision()
        {
            if (Map == null) return null;
            IntVec3 nextCell = (currentRealPos + currentVelocity * (def.projectile.speed / 100f)).ToIntVec3();
            if (!nextCell.InBounds(Map)) return null;
            foreach (Thing t in Map.thingGrid.ThingsAt(nextCell))
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

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamed("Mote_REIMU_SpreadTalismanTrail");
            Mote_REIMU_SpreadTalismanTrail trail = (Mote_REIMU_SpreadTalismanTrail)ThingMaker.MakeThing(moteDef);
            trail.exactPosition = currentRealPos;
            trail.initialRotation = spinAngle;
            GenSpawn.Spawn(trail, Position, Map);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
        }
    }

    public class Mote_REIMU_SpreadTalismanTrail : Thing
    {
        public Vector3 exactPosition;
        public float initialRotation;
        private int age = 0;
        private const int MaxAge = 15;
        private const float StartScale = 1f;
        private const float EndScale = 3f;
        private const float SpinSpeed = 20f;

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
            float animScale = Mathf.Lerp(StartScale, EndScale, progress);
            float alpha = 1f - progress;

            drawLoc = exactPosition;
            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Material mat = FadedMaterialPool.FadedVersionOf(SpreadTalismanGraphics.TrailMat, alpha);
            float currentAngle = initialRotation + age * SpinSpeed;
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);

            Vector2 baseSize = def.graphicData.drawSize;
            Vector3 scaleVec = new Vector3(baseSize.x * animScale, 1f, baseSize.y * animScale);
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, rot, scaleVec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}