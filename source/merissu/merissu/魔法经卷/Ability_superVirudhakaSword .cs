using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_superVirudhakaSword : Ability
    {
        public Ability_superVirudhakaSword() : base() { }
        public Ability_superVirudhakaSword(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast => base.CanCast;

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null)
                return false;


            if (def.verbProperties.soundCast != null)
            {
                def.verbProperties.soundCast.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            Vector3 dir = (target.Cell.ToVector3Shifted() - pawn.DrawPos).normalized;
            if (dir == Vector3.zero)
                dir = pawn.Rotation.FacingCell.ToVector3();

            Vector3 spawnOffset = pawn.DrawPos + dir * 0.8f;

            Thing_VirudhakaShockwave shockwave = (Thing_VirudhakaShockwave)GenSpawn.Spawn(
                ThingDef.Named("Thing_VirudhakaShockwave"),
                spawnOffset.ToIntVec3(),
                pawn.Map
            );
            shockwave.Initialize(spawnOffset, dir);

            Thing_VirudhakaSuperProjectile proj = (Thing_VirudhakaSuperProjectile)GenSpawn.Spawn(
                ThingDef.Named("Thing_VirudhakaSuperProjectile"),
                pawn.Position,
                pawn.Map
            );
            proj.Initialize(pawn, dir);

            return true;
        }
    }

    public class Thing_VirudhakaSuperProjectile : Thing
    {
        private Pawn launcher;
        private Vector3 direction;
        private Vector3 exactPos;
        private float speed = 1.3f;
        private int age = 0;
        private bool isBounced = false;
        private Vector3 bounceDir;
        private float spinAngle = 0f;
        private float spinSpeed = 0f;
        private int bounceAge = 0;
        private const int MaxBounceAge = 30;
        private const float CollisionRadius = 1.3f;
        private HashSet<Thing> alreadyHit = new HashSet<Thing>();

        private const int DamageAmountPawn = 20;    
        private const int DamageAmountOther = 80;   
        private const int BuildingHitPointsThreshold = 850;

        public void Initialize(Pawn launcher, Vector3 dir)
        {
            this.launcher = launcher;
            this.direction = dir.normalized;
            this.exactPos = launcher.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null) return;

            age++;

            if (isBounced)
            {
                bounceAge++;
                if (bounceAge >= MaxBounceAge)
                {
                    Destroy();
                    return;
                }

                exactPos += bounceDir * (speed * 0.4f);
                spinAngle += spinSpeed;

                IntVec3 bCell = exactPos.ToIntVec3();
                if (bCell.InBounds(Map))
                    Position = bCell;

                return;
            }

            exactPos += direction * speed;
            IntVec3 centerCell = exactPos.ToIntVec3();

            if (!centerCell.InBounds(Map))
            {
                Destroy();
                return;
            }

            Position = centerCell;

            Building edifice = centerCell.GetEdifice(Map);
            if (edifice != null && (edifice.def.Fillage == FillCategory.Full || edifice.def.passability == Traversability.Impassable))
            {
                if (edifice.def.useHitPoints && edifice.HitPoints < BuildingHitPointsThreshold)
                {
                    edifice.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    isBounced = true;
                    bounceDir = (-direction + new Vector3(Rand.Range(-0.4f, 0.4f), 0f, Rand.Range(-0.4f, 0.4f))).normalized;
                    spinSpeed = Rand.Range(20f, 35f);
                    bounceAge = 0;
                    return;
                }
            }

            int trailCount = Rand.Range(1, 3);
            for (int i = 0; i < trailCount; i++)
            {
                float distanceBehind = Rand.Range(0f, 2.0f);
                Vector3 perp = new Vector3(-direction.z, 0f, direction.x).normalized;
                float sideOffset = Rand.Range(-0.4f, 0.4f);

                Vector3 trailSpawnPos = exactPos - (direction * distanceBehind) + (perp * sideOffset);
                IntVec3 trailCell = trailSpawnPos.ToIntVec3();

                if (trailCell.InBounds(Map))
                {
                    Thing_VirudhakaTrail trail = (Thing_VirudhakaTrail)GenSpawn.Spawn(
                        ThingDef.Named("Thing_VirudhakaTrail"),
                        trailCell,
                        Map
                    );
                    trail.Initialize(trailSpawnPos, direction);
                }
            }

            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(centerCell, CollisionRadius, true);
            foreach (IntVec3 c in cells)
            {
                if (!c.InBounds(Map)) continue;

                List<Thing> things = c.GetThingList(Map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];

                    if (t == this || t == launcher) continue;
                    if (alreadyHit.Contains(t)) continue;
                    if (t.def.category == ThingCategory.Item) continue;
                    if (t.def.category == ThingCategory.Plant) continue;

                    alreadyHit.Add(t);

                    if (t is Pawn pawn)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (pawn.Dead) break;
                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Cut,
                                DamageAmountPawn,
                                999f,
                                -1f,
                                launcher
                            );
                            pawn.TakeDamage(dinfo);
                        }
                    }
                    else
                    {
                        DamageInfo dinfo = new DamageInfo(
                            DamageDefOf.Blunt,
                            DamageAmountOther,
                            999f,
                            -1f,
                            launcher
                        );
                        t.TakeDamage(dinfo);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float angle = direction.AngleFlat();
            float offset = -90f;

            Quaternion rot;
            if (isBounced)
            {
                rot = Quaternion.AngleAxis(angle + offset + spinAngle, Vector3.up);
            }
            else
            {
                rot = Quaternion.AngleAxis(angle + offset, Vector3.up);
            }

            float alpha = 1.0f;
            if (isBounced)
            {
                alpha = 1.0f - ((float)bounceAge / MaxBounceAge);
            }

            Material baseMat = MaterialPool.MatFrom("Other/ShinkiRecitation/bladeA0000", ShaderDatabase.MoteGlow);
            if (baseMat != null)
            {
                Material fadedMat = FadedMaterialPool.FadedVersionOf(baseMat, alpha);
                Vector3 pos = exactPos;
                pos.y = AltitudeLayer.Projectile.AltitudeFor();

                Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, new Vector3(5f, 1f, 5f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, fadedMat, 0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref exactPos, "exactPos");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref isBounced, "isBounced");
            Scribe_Values.Look(ref bounceDir, "bounceDir");
            Scribe_Values.Look(ref spinAngle, "spinAngle");
            Scribe_Values.Look(ref spinSpeed, "spinSpeed");
            Scribe_Values.Look(ref bounceAge, "bounceAge");
            Scribe_Collections.Look(ref alreadyHit, "alreadyHit", LookMode.Reference);
        }
    }
}