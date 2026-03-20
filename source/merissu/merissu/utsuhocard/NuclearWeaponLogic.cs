using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Verb_ShootNuclearFireball_Custom : Verb
    {
        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;
            if (pawn == null || pawn.Map == null) return false;
            LocalTargetInfo target = this.currentTarget;

            Projectile proj = (Projectile)GenSpawn.Spawn(
                ThingDef.Named("Projectile_NuclearFireball"),
                pawn.Position,
                pawn.Map
            );

            proj.Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.All, false, null, null);
            return true;
        }
    }

    public class Projectile_NuclearFireball_Custom : Projectile
    {
        private const float CollisionRadius = 1.2f;
        private bool animSpawned = false;

        protected override void Tick()
        {
            base.Tick();
            if (Map == null || !this.Spawned) return;

            if (!animSpawned)
            {
                Thing_NuclearFireballFollowingAnim anim = (Thing_NuclearFireballFollowingAnim)ThingMaker.MakeThing(ThingDef.Named("NuclearFireball_FollowingAnim"));
                anim.target = this;
                GenSpawn.Spawn(anim, Position, Map);
                animSpawned = true;
            }

            if (Find.TickManager.TicksGame % 4 == 0)
            {
                CellRect rect = CellRect.CenteredOn(Position, 8, 8);
                foreach (IntVec3 cell in rect)
                {
                    if (cell.InBounds(Map))
                    {
                        FireUtility.TryStartFireIn(cell, Map, 0.2f, launcher);
                    }
                }
            }

            CheckCollision();
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
            base.Impact(hitThing, blockedByShield);

            GenExplosion.DoExplosion(pos, map, 10f, DamageDefOf.Bomb, launcher, 60);

            Thing pillar = ThingMaker.MakeThing(ThingDef.Named("NuclearFireball_PillarEffect"));
            GenSpawn.Spawn(pillar, pos, map);
        }
    }

    public class Thing_NuclearFireballFollowingAnim : Thing
    {
        public Thing target;
        private int age = 0;
        private const int TicksPerFrame = 1;
        private const int TotalFrames = 20;
        private float drawScale = 18.0f; 

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (target == null || target.Destroyed || !target.Spawned)
            {
                this.Destroy();
                return;
            }

            this.Position = target.Position;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (target == null) return;

            int totalTicks = TicksPerFrame * TotalFrames;
            int frame = (age % totalTicks) / TicksPerFrame;

            Material mat = MaterialPool.MatFrom($"Projectiles/BoomNuclearControlRod/bulletDa{frame:D3}", ShaderDatabase.Mote);

            Vector3 pos = target.DrawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 s = new Vector3(drawScale, 1f, drawScale);

            float baseAngle = target.Rotation.AsAngle;
            if (target is Projectile proj)
            {
                baseAngle = proj.ExactRotation.eulerAngles.y;
            }

            Quaternion finalRotation = Quaternion.AngleAxis(baseAngle - 90f, Vector3.up);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, finalRotation, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_NuclearFireballPillar : Thing
    {
        private int age = 0;
        private const int TicksPerFrame = 2;
        private const int TotalFrames = 20;
        private float drawScale = 20.0f;

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (age % 1 == 0 && this.Spawned)
            {
                List<Thing> targets = new List<Thing>(GenRadial.RadialDistinctThingsAround(Position, Map, 10f, true));
                foreach (Thing t in targets)
                {
                    if (t is Pawn p && !p.Dead)
                    {
                        p.TakeDamage(new DamageInfo(DamageDefOf.Burn, 12f, 0, -1, null));
                        p.TryAttachFire(0.3f, null);
                    }
                }
            }

            if (age >= TicksPerFrame * TotalFrames)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Min(age / TicksPerFrame, TotalFrames - 1);
            Material mat = MaterialPool.MatFrom($"Projectiles/NuclearControlRodBoom/bulletKa{frame:D3}", ShaderDatabase.MoteGlow);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 s = new Vector3(drawScale, 1f, drawScale / 2f);
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