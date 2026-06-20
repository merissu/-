using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;

namespace merissu
{
    public class Thing_StarlightOrb : Thing
    {
        private Pawn owner;
        private float angle;
        private int rotationDir = 1;
        private Material laserMat;
        private Vector3 laserEnd;

        private int lifeTicks;
        private int laserTick;
        private int shootTick;
        private int soundTick; 
        private int projectileIndex;

        private const int MaxLifeTicks = 600;
        private const int ShootInterval = 3;
        private const int SoundInterval = 90; 
        private const float Radius = 4f;
        private const float RotateSpeed = 1f;
        private const float LaserRange = 250f;

        private static readonly ThingDef[] ProjectileDefs =
        {
            DefDatabase<ThingDef>.GetNamed("StarlightProjectileA"),
            DefDatabase<ThingDef>.GetNamed("StarlightProjectileB"),
            DefDatabase<ThingDef>.GetNamed("StarlightProjectileC"),
            DefDatabase<ThingDef>.GetNamed("StarlightProjectileD"),
            DefDatabase<ThingDef>.GetNamed("StarlightProjectileE")
        };

        public void Init(Pawn owner, float startAngle, Material laserMat, int rotationDir)
        {
            this.owner = owner;
            this.angle = startAngle;
            this.laserMat = laserMat;
            this.rotationDir = rotationDir;
            this.lifeTicks = MaxLifeTicks;
            this.soundTick = 0; 
        }

        protected override void Tick()
        {
            base.Tick();

            if (owner == null || owner.Dead || owner.Destroyed)
            {
                Destroy();
                return;
            }

            lifeTicks--;
            if (lifeTicks <= 0)
            {
                Destroy();
                return;
            }

            angle = (angle + RotateSpeed * rotationDir) % 360f;

            laserTick++;
            if (laserTick % 2 == 0)
            {
                UpdateLaser();
            }

            shootTick++;
            if (shootTick >= ShootInterval)
            {
                shootTick = 0;
                FireProjectile();
            }

            soundTick++;
            if (soundTick >= SoundInterval)
            {
                soundTick = 0;
                PlayCustomSound();
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (owner == null) return base.DrawPos;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(rad) * Radius,
                    0f,
                    Mathf.Sin(rad) * Radius
                );
                return owner.DrawPos + offset;
            }
        }


        private void UpdateLaser()
        {
            if (Map == null) return;

            Vector3 start = DrawPos;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            laserEnd = start + dir * LaserRange;

            IntVec3 from = start.ToIntVec3();
            IntVec3 to = laserEnd.ToIntVec3();

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(from, to))
            {
                if (!cell.InBounds(Map)) break;

                Building b = cell.GetFirstBuilding(Map);
                if (b != null && b.def.Fillage == FillCategory.Full)
                {
                    laserEnd = b.DrawPos;
                    break;
                }

                List<Thing> list = cell.GetThingList(Map);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == this || t == owner) continue;

                    if (t is Pawn p)
                    {
                        if (owner.Faction != null && p.Faction != null && !p.Faction.HostileTo(owner.Faction))
                            continue;
                        ApplyDamage(p);
                    }
                    else if (t.def.category == ThingCategory.Item)
                    {
                        ApplyDamage(t);
                    }
                }
            }
        }

        private void FireProjectile()
        {
            if (Map == null || owner == null) return;

            ThingDef projDef = ProjectileDefs[projectileIndex];
            projectileIndex = (projectileIndex + 1) % ProjectileDefs.Length;

            Vector3 dir = (owner.DrawPos - DrawPos).normalized;
            Vector3 spawnPos = DrawPos + dir * 0.1f;
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            if (!spawnCell.InBounds(Map)) return;

            Projectile proj = (Projectile)ThingMaker.MakeThing(projDef);
            if (proj == null) return;

            GenSpawn.Spawn(proj, spawnCell, Map);

            Vector3 farPoint = DrawPos + dir * 50f;
            LocalTargetInfo target = new LocalTargetInfo(farPoint.ToIntVec3());

            proj.Launch(
                owner,                      
                spawnPos,                    
                target,                     
                target,                      
                ProjectileHitFlags.None,    
                true,                       
                null                        
            );
        }

        private void ApplyDamage(Thing target)
        {
            target.TakeDamage(new DamageInfo(
                DamageDefOf.Flame,
                20f,
                1f,
                -1f,
                instigator: owner
            ));
        }

        private void PlayCustomSound()
        {
            SoundDef sound = SoundDef.Named("StarlightTyphoon");
            if (sound != null && Map != null)
            {
                sound.PlayOneShot(new TargetInfo(Position, Map));
            }
        }


        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (laserMat == null || Map == null) return;

            StarlightLaserDrawer.DrawLaser(
                drawLoc,
                laserEnd,
                laserMat,
                2f
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref rotationDir, "rotationDir");
            Scribe_Values.Look(ref lifeTicks, "lifeTicks");
            Scribe_Values.Look(ref projectileIndex, "projectileIndex");
            Scribe_Values.Look(ref soundTick, "soundTick", 0);
        }
    }
}