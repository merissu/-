using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Projectile_Missile : Projectile
    {
        private const float CollisionRadius = 0.8f; 

        protected override void Tick()
        {
            base.Tick();
            if (Map == null || !this.Spawned) return;

            CheckCollision();
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false, Thing equipment = null, ThingDef targetWeaponDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, targetWeaponDef);

            SoundDef missileSound = SoundDef.Named("Missile");
            if (missileSound != null)
            {
                missileSound.PlayOneShotOnCamera(launcher.Map);
            }
        }
        private void CheckCollision()
        {
            List<Thing> targets = new List<Thing>(GenRadial.RadialDistinctThingsAround(Position, Map, CollisionRadius, true));
            foreach (Thing t in targets)
            {
                if (t != launcher && (t is Pawn || t is Building))
                {
                    if (t is Pawn p && p.Downed) continue;

                    this.Impact(t);
                    break;
                }
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 pos = Position;

            int damageAmount = (int)DamageAmount;
            Thing launcherRef = launcher;

            base.Impact(hitThing, blockedByShield);

            GenExplosion.DoExplosion(
                pos,
                map,
                3.0f,               
                DamageDefOf.Bomb,
                launcherRef,
                damageAmount        
            );


            GenExplosion.DoExplosion(
                pos,
                map,
                2.5f,               
                DamageDefOf.Flame,
                launcherRef,
                damageAmount / 2    
            );

            Thing anim = ThingMaker.MakeThing(ThingDef.Named("Missile_ExplosionAnim"));
            GenSpawn.Spawn(anim, pos, map);
        }
    }

    public class Thing_MissileExplosionAnim : Thing
    {
        private int age = 0;
        private const int TicksPerFrame = 3; 
        private const int TotalFrames = 8;   
        private float drawSize = 6.0f;       

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (age >= TicksPerFrame * TotalFrames)
            {
                this.Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Min(age / TicksPerFrame, TotalFrames - 1);

            Material mat = MaterialPool.MatFrom($"Projectiles/MissileExplo/Effect{frame:D3}", ShaderDatabase.MoteGlow);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor(); 

            Vector3 s = new Vector3(drawSize, 1f, drawSize);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, s);

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age", 0);
        }
    }
}