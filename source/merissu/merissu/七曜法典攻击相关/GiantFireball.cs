using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class GiantFireballGraphics
    {
        public static readonly Material[] BlastFrames = new Material[9];

        static GiantFireballGraphics()
        {
            for (int i = 0; i < 9; i++)
            {
                BlastFrames[i] = MaterialPool.MatFrom($"Projectiles/fireball/BulletBc{i:D3}", ShaderDatabase.MoteGlow);
            }
        }
    }

    public class Thing_DirectionalFireBlast : Thing
    {
        public float exactRotation;
        public Vector3 exactPosition;
        private int ticks = 0;
        private const int TicksPerFrame = 3; 
        private const int TotalFrames = 9;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (exactPosition == Vector3.zero) exactPosition = this.Position.ToVector3Shifted();
        }

        protected override void Tick()
        {
            base.Tick();
            ticks++;
            if (ticks >= TotalFrames * TicksPerFrame)
            {
                this.Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = ticks / TicksPerFrame;
            if (frame >= TotalFrames) frame = TotalFrames - 1;

            Material mat = GiantFireballGraphics.BlastFrames[frame];
            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                exactPosition,
                Quaternion.AngleAxis(exactRotation, Vector3.up),
                new Vector3(2.5f, 1f, 2.5f) 
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_GiantFireballShockwave : Thing
    {
        private int age = 0;
        private const int MaxAge = 12; 
        public Vector3 exactPosition;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = (float)age / MaxAge;

            float scale = Mathf.Lerp(2f, 18f, progress);

            float alpha = 1f - progress;

            Material shockMat = FadedMaterialPool.FadedVersionOf(
                FireballGraphics.ShockwaveMat,
                alpha
            );

            drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 outer = Matrix4x4.TRS(
                drawLoc,
                Quaternion.identity,
                new Vector3(scale, 1f, scale)
            );

            Graphics.DrawMesh(MeshPool.plane10, outer, shockMat, 0);

            Material centerMat = FadedMaterialPool.FadedVersionOf(
                FireballGraphics.CenterMat,
                alpha
            );

            Matrix4x4 center = Matrix4x4.TRS(
                drawLoc,
                Quaternion.identity,
                Vector3.one * Mathf.Lerp(1f, 3f, progress)
            );

            Graphics.DrawMesh(MeshPool.plane10, center, centerMat, 0);
        }
    }
    public class Projectile_GiantFireball : Projectile
    {
        private int ticks = 0;
        private const int TicksPerFrame = 3;
        private const float DrawScale = 3.5f; 

        protected override void Tick()
        {
            base.Tick();
            ticks++;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = (ticks / TicksPerFrame) % 11;
            Material mat = FireballGraphics.FireballFrames[frame];

            drawLoc.y = AltitudeLayer.Projectile.AltitudeFor();
            float rotAngle = ExactRotation.eulerAngles.y;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawLoc,
                Quaternion.AngleAxis(rotAngle, Vector3.up),
                new Vector3(DrawScale, 1f, DrawScale)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = this.Map;
            Vector3 pos = this.ExactPosition;

            if (map != null)
            {
                SoundDef.Named("FireballDestruction").PlayOneShot(new TargetInfo(this.Position, map));

                Thing shockwave = ThingMaker.MakeThing(ThingDef.Named("GiantFireball_Shockwave"));
                GenSpawn.Spawn(shockwave, pos.ToIntVec3(), map);

                if (shockwave is Thing_GiantFireballShockwave sw)
                {
                    sw.exactPosition = pos;
                }
            }

            base.Destroy(mode);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            Vector3 pos = this.ExactPosition;
            IntVec3 center = this.Position;

            base.Impact(hitThing, blockedByShield); 

            if (map != null)
            {
                IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(center, 1.9f, true);

                foreach (IntVec3 cell in cells)
                {
                    if (!cell.InBounds(map)) continue;

                    FireUtility.TryStartFireIn(cell, map, 0.75f, this.launcher, null);

                    List<Thing> things = cell.GetThingList(map).ListFullCopy();

                    foreach (Thing t in things)
                    {
                        if (t is Pawn pawn && pawn != this.launcher)
                        {
                            float damageAmount = this.def.projectile.GetDamageAmount(this.launcher);

                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Flame,
                                damageAmount,
                                0.5f,
                                -1f,
                                this.launcher,
                                null,
                                this.equipmentDef
                            );

                            pawn.TakeDamage(dinfo);

                            pawn.TryAttachFire(1.5f, this.launcher);

                            ApplyKnockback(pawn, pos, 3);
                        }
                        else if (t.def.category == ThingCategory.Building || t.def.category == ThingCategory.Item)
                        {
                            float damageAmount = this.def.projectile.GetDamageAmount(this.launcher);
                            t.TakeDamage(new DamageInfo(DamageDefOf.Flame, damageAmount, 0f, -1f, this.launcher));
                        }
                    }
                }

                GenExplosion.DoExplosion(
                    center,
                    map,
                    1.9f,
                    DamageDefOf.Flame,
                    this.launcher
                );
            }
        }

        private void ApplyKnockback(Pawn victim, Vector3 explodeCenter, int pushDist)
        {
            if (victim == null || victim.Dead || victim.Map == null) return;

            Map map = victim.Map;

            Vector3 dir = (victim.DrawPos - explodeCenter);
            if (dir == Vector3.zero) dir = Vector3.forward;
            dir.Normalize();

            IntVec3 targetCell = (victim.Position.ToVector3() + dir * pushDist).ToIntVec3();

            if (!targetCell.InBounds(map) || !targetCell.Walkable(map))
            {
                if (!CellFinder.TryFindRandomCellNear(targetCell, map, 2, c => c.Walkable(map), out targetCell))
                {
                    targetCell = victim.Position;
                }
            }

            if (targetCell == victim.Position) return;

            ThingDef flyerDef = DefDatabase<ThingDef>.GetNamed("PawnFlyer", false);

            if (flyerDef != null)
            {
                PawnFlyer flyer = PawnFlyer.MakeFlyer(
                    flyerDef,
                    victim,
                    targetCell,
                    null,
                    null,
                    false,
                    null,
                    null,
                    LocalTargetInfo.Invalid
                );

                if (flyer != null)
                {
                    GenSpawn.Spawn(flyer, victim.Position, map);
                }
            }
            else
            {
                victim.Position = targetCell;
                victim.Notify_Teleported(true, true);
            }

            if (victim.stances?.stunner != null)
            {
                victim.stances.stunner.StunFor(60, this.launcher, false, false);
            }

            victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 2f, 0, -1, this.launcher));
        }
    }
}