using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class WindBulletGraphics
    {
        public static readonly Material ChargeMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAc000", ShaderDatabase.MoteGlow);
        public static readonly Material RingMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAg000", ShaderDatabase.MoteGlow);
        public static readonly Material ScatterMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAa000", ShaderDatabase.MoteGlow);
        public static readonly Material PathTrailMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAd000", ShaderDatabase.MoteGlow);
        public static readonly Material ImpactMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAe000", ShaderDatabase.MoteGlow);
    }
    public class AttackMode_WindBullet : GrimoireAttackMode
    {
        public override string ModeName => "WindBullet";
        protected override string ProjectileDefName => "Projectile_WindBullet";
        protected override string SoundDefName => "WindBullet";

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.5f;

        private Thing currentChargeMote;

        public override void OnWarmupStart(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return;

            SoundDef.Named("WindBulletCharging")?.PlayOneShot(new TargetInfo(caster.Position, map));

            currentChargeMote = ThingMaker.MakeThing(ThingDef.Named("Mote_WindBulletCharge"));
            GenSpawn.Spawn(currentChargeMote, caster.Position, map);
            if (currentChargeMote is Thing_WindBulletCharge chargeObj)
            {
                chargeObj.AttachTo(caster);
            }
        }

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            if (currentChargeMote != null && !currentChargeMote.Destroyed)
            {
                currentChargeMote.Destroy();
                currentChargeMote = null;
            }

            Vector3 casterPos = caster.DrawPos;
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            if (target.Thing != null) targetPos = target.Thing.DrawPos;

            Vector3 dir = (targetPos - casterPos).normalized;
            Vector3 spawnPos = casterPos + dir * 1f;
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            Thing ringAnim = ThingMaker.MakeThing(ThingDef.Named("Mote_WindRingShockwave"));
            GenSpawn.Spawn(ringAnim, spawnCell, map);
            if (ringAnim is Thing_WindRingShockwave ring)
            {
                ring.exactPosition = spawnPos;
                ring.forwardDir = dir;
            }

            for (int i = 0; i < 5; i++)
            {
                Thing scatter = ThingMaker.MakeThing(ThingDef.Named("Mote_WindScatter"));
                GenSpawn.Spawn(scatter, spawnCell, map);
                if (scatter is Thing_WindScatter s)
                {
                    s.exactPosition = spawnPos;
                    float randomAngle = dir.AngleFlat() + Rand.Range(-45f, 45f);
                    s.velocity = Vector3Utility.FromAngleFlat(randomAngle) * Rand.Range(0.05f, 0.15f);
                }
            }

            Projectile proj = (Projectile)GenSpawn.Spawn(ProjectileDef, spawnCell, map);
            proj.Launch(caster, spawnPos, target, target, ProjectileHitFlags.All, false, null, null);

            return true;
        }
    }

    public class Thing_WindBulletCharge : Thing
    {
        private Pawn caster;
        private float angleCW = 0f;
        private float angleCCW = 0f;

        public void AttachTo(Pawn p) => caster = p;

        protected override void Tick()
        {
            if (caster == null ||
                caster.Dead ||
                caster.Downed ||
                !(caster.stances?.curStance is Stance_Warmup))
            {
                if (!this.Destroyed)
                    this.Destroy();
                return;
            }            
            this.Position = caster.Position;
            angleCW += 8f;   
            angleCCW -= 5f;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;
            Vector3 drawPos = caster.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            drawPos += caster.Rotation.FacingCell.ToVector3() * 0.5f; 

            Matrix4x4 matCW = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, angleCW, 0), Vector3.one);
            Graphics.DrawMesh(MeshPool.plane10, matCW, WindBulletGraphics.ChargeMat, 0);

            Material matCCW = FadedMaterialPool.FadedVersionOf(WindBulletGraphics.ChargeMat, 0.6f);
            Matrix4x4 matCCW_Matrix = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, angleCCW, 0), new Vector3(-2f, 1f, 2f));
            Graphics.DrawMesh(MeshPool.plane10, matCCW_Matrix, matCCW, 0);
        }
    }

    public class Thing_WindRingShockwave : Thing
    {
        public Vector3 exactPosition;
        public Vector3 forwardDir;
        private int age = 0;
        private const int MaxAge = 25;

        protected override void Tick()
        {
            age++;
            if (age >= MaxAge && !this.Destroyed) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float alpha = 1f - progress;
            Material mat = FadedMaterialPool.FadedVersionOf(WindBulletGraphics.RingMat, alpha);

            float baseScaleX = Mathf.Lerp(1f, 4f, progress);
            float baseScaleZ = baseScaleX * 0.5f;

            for (int i = 0; i < 3; i++)
            {
                float scaleX = baseScaleX * Mathf.Pow(1.5f, i);
                float scaleZ = baseScaleZ * Mathf.Pow(1.5f, i);
                Vector3 ringPos = exactPosition + forwardDir * (progress * 2f + (i * 0.5f));
                ringPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + (i * 0.01f);

                Matrix4x4 matrix = Matrix4x4.TRS(ringPos, Quaternion.Euler(0, forwardDir.AngleFlat(), 0), new Vector3(scaleX, 1f, scaleZ));
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }
    }
    public class Thing_WindScatter : Thing
    {
        public Vector3 exactPosition;
        public Vector3 velocity;
        private int age = 0;
        private const int MaxAge = 20;

        protected override void Tick()
        {
            exactPosition += velocity;
            age++;
            if (age >= MaxAge && !this.Destroyed) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / MaxAge);
            Material mat = FadedMaterialPool.FadedVersionOf(WindBulletGraphics.ScatterMat, alpha);
            Vector3 drawPos = exactPosition;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(0.8f, 1f, 0.8f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
    public class Thing_WindImpact : Thing
    {
        public Vector3 exactPosition;
        private int age = 0;
        private const int MaxAge = 15;
        private float angle = 0f;

        protected override void Tick()
        {
            age++;
            angle += 15f;
            if (age >= MaxAge && !this.Destroyed) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;
            float alpha = 1f - progress;
            float scale = Mathf.Lerp(2f, 6f, progress);

            Material mat = FadedMaterialPool.FadedVersionOf(WindBulletGraphics.ImpactMat, alpha);
            Vector3 drawPos = exactPosition;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, angle, 0), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
    public class Thing_WindPathTrail : Thing
    {
        public Vector3 exactPosition;
        public Vector3 velocity;
        public float exactRotation; 
        private int age = 0;
        private const int MaxAge = 15;

        protected override void Tick()
        {
            exactPosition += velocity;
            age++;
            if (age >= MaxAge && !this.Destroyed) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / MaxAge);
            Material mat = FadedMaterialPool.FadedVersionOf(WindBulletGraphics.PathTrailMat, alpha);

            Vector3 drawPos = exactPosition;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.01f; 
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, exactRotation, 0), Vector3.one);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
    public class Projectile_WindBullet : Projectile
    {
        protected override void Tick()
        {
            base.Tick();
            if (this.Map != null && Find.TickManager.TicksGame % 1 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Thing pathTrail = ThingMaker.MakeThing(ThingDef.Named("Mote_WindPathTrail"));
                    GenSpawn.Spawn(pathTrail, this.Position, this.Map);

                    if (pathTrail is Thing_WindPathTrail trail)
                    {
                        trail.exactPosition = this.DrawPos;

                        float baseAngle = this.ExactRotation.eulerAngles.y;
                        float randomAngle = baseAngle + Rand.Range(-120f, 120f);

                        Vector3 dir = Vector3Utility.FromAngleFlat(randomAngle);

                        trail.velocity =
                            dir * Rand.Range(0.03f, 0.10f)
                            + new Vector3(Rand.Range(-0.02f, 0.02f), 0, Rand.Range(-0.02f, 0.02f));
                    }
                }
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            Vector3 hitPos = this.DrawPos;
            Pawn caster = this.launcher as Pawn;

            Thing impactObj = ThingMaker.MakeThing(ThingDef.Named("Mote_WindImpact"));
            GenSpawn.Spawn(impactObj, this.Position, map);
            if (impactObj is Thing_WindImpact imp) imp.exactPosition = hitPos;

            for (int i = 0; i < 5; i++)
            {
                Thing scatter = ThingMaker.MakeThing(ThingDef.Named("Mote_WindScatter"));
                GenSpawn.Spawn(scatter, this.Position, map);
                if (scatter is Thing_WindScatter s)
                {
                    s.exactPosition = hitPos;
                    float randomAngle = Rand.Range(0f, 360f); 
                    s.velocity = Vector3Utility.FromAngleFlat(randomAngle) * Rand.Range(0.05f, 0.15f);
                }
            }

            if (hitThing != null && !blockedByShield)
            {
                hitThing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, this.DamageAmount, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, caster));

                if (hitThing is Pawn victim && !victim.Dead)
                {
                    Vector3 direction = (victim.Position.ToVector3() - this.launcher.Position.ToVector3()).normalized;
                    if (direction == Vector3.zero) direction = Vector3.forward;

                    IntVec3 startCell = victim.Position;
                    IntVec3 targetCell = startCell;
                    float maxDistance = 30f; 

                    for (float distance = 0.5f; distance <= maxDistance; distance += 0.5f)
                    {
                        IntVec3 checkCell = (startCell.ToVector3() + direction * distance).ToIntVec3();

                        if (!checkCell.InBounds(map))
                        {
                            break;
                        }

                        if (!checkCell.Walkable(map) || checkCell.Impassable(map))
                        {
                            break;
                        }

                        targetCell = checkCell;
                    }

                    if (targetCell.InBounds(map) && targetCell != victim.Position)
                    {
                        PawnFlyer_WindKnockback flyer = (PawnFlyer_WindKnockback)PawnFlyer.MakeFlyer(
                            ThingDef.Named("WindKnockbackFlyer"), victim, targetCell, null, null, false, null, null, LocalTargetInfo.Invalid);

                        if (flyer != null)
                        {
                            flyer.instigator = caster;
                            GenSpawn.Spawn(flyer, startCell, map); 
                        }
                    }
                }
            }
            base.Impact(hitThing, blockedByShield);
        }
    }
    public class PawnFlyer_WindKnockback : PawnFlyer
    {
        public Pawn instigator;

        protected override void RespawnPawn()
        {
            base.RespawnPawn();

            if (FlyingPawn == null || FlyingPawn.Dead) return;

            Pawn pawn = FlyingPawn;
            Pawn inst = instigator;

            if (!pawn.Dead)
            {
                pawn.TakeDamage(new DamageInfo(
                    DamageDefOf.Blunt,
                    20f,
                    0f,
                    -1f,
                    inst));
            }
        }
    }
}