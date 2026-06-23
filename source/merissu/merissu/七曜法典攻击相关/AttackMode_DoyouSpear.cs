using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class DoyouSpearGraphics
    {
        public static readonly Material FogMat = MaterialPool.MatFrom("Projectiles/WindBullet/spellBulletAa000", ShaderDatabase.MoteGlow);
        public static readonly Material BulletMat = MaterialPool.MatFrom("Projectiles/DoyouSpear/bulletIc000", ShaderDatabase.MoteGlow);
        public static readonly Material TrailMat = MaterialPool.MatFrom("Projectiles/DoyouSpear/bulletI000", ShaderDatabase.MoteGlow);
        public static readonly Material DebrisMat = MaterialPool.MatFrom("Projectiles/DoyouSpear/bulletId000", ShaderDatabase.MoteGlow);
        public static readonly Material[] ImpactAnimMats = new Material[20];

        static DoyouSpearGraphics()
        {
            for (int i = 0; i < 20; i++)
            {
                ImpactAnimMats[i] = MaterialPool.MatFrom($"Projectiles/DoyouSpear/bulletIa{i:D3}", ShaderDatabase.MoteGlow);
            }
        }
    }

    public class AttackMode_DoyouSpear : GrimoireAttackMode
    {
        public override string ModeName => "DoyouSpear";
        protected override string ProjectileDefName => "Projectile_WaterJadePiercing";
        protected override string SoundDefName => "DoyouSpear";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 0.5f;

        public override void OnWarmupStart(Verb_RandomElementalShoot verb, LocalTargetInfo target) { }

        public override bool OverrideCastShot(Verb_RandomElementalShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;

            CastSound?.PlayOneShot(new TargetInfo(caster.Position, map));

            Pawn randomEnemy = map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(caster.Faction) && !p.Dead && p.Position.DistanceTo(caster.Position) <= verb.verbProps.range)
                .RandomElementWithFallback(null);

            IntVec3 targetCell = randomEnemy != null ? randomEnemy.Position : target.Cell;

            Thing_DoyouSpearManager manager = (Thing_DoyouSpearManager)ThingMaker.MakeThing(ThingDef.Named("Mote_DoyouSpearManager"));
            manager.Setup(caster, targetCell);
            GenSpawn.Spawn(manager, targetCell, map);

            return true;
        }
    }
    public class Thing_DoyouSpearManager : Thing
    {
        private Pawn caster;
        private IntVec3 centerCell;
        private int age = 0;
        private const float AreaRadius = 5.5f;
        private const int TotalBullets = 20;
        private List<IntVec3> bulletCells;
        private int bulletsSpawned = 0;

        public void Setup(Pawn caster, IntVec3 centerCell)
        {
            this.caster = caster;
            this.centerCell = centerCell;
            bulletCells = GenRadial.RadialCellsAround(centerCell, AreaRadius, true).ToList();
            bulletCells.Shuffle();
        }

        protected override void Tick()
        {
            age++;
            Map map = this.Map;

            if (age == 1)
            {
                var fogPositions = GenRadial.RadialCellsAround(centerCell, 5.5f, true).ToList();
                fogPositions.Shuffle();
                int fogCount = Mathf.Min(15, fogPositions.Count);
                for (int i = 0; i < fogCount; i++)
                {
                    IntVec3 fogPos = fogPositions[i];
                    Thing_DoyouSpearFog fog = (Thing_DoyouSpearFog)ThingMaker.MakeThing(ThingDef.Named("Mote_DoyouSpearFog"));
                    fog.exactPosition = fogPos.ToVector3Shifted() + new Vector3(Rand.Range(-0.8f, 0.8f), 0, Rand.Range(-0.8f, 0.8f));
                    GenSpawn.Spawn(fog, fogPos, map);
                }
            }

            if (age >= 2 && bulletsSpawned < TotalBullets && bulletCells != null)
            {
                int spawnThisTick = Mathf.Min(4, TotalBullets - bulletsSpawned);
                for (int i = 0; i < spawnThisTick; i++)
                {
                    int index = bulletsSpawned;
                    if (index < bulletCells.Count)
                    {
                        IntVec3 cell = bulletCells[index];
                        Thing_DoyouSpearProjectile spear = (Thing_DoyouSpearProjectile)ThingMaker.MakeThing(ThingDef.Named("Mote_DoyouSpearProjectile"));
                        spear.Setup(caster);
                        GenSpawn.Spawn(spear, cell, map);
                    }
                    bulletsSpawned++;
                }
            }

            if (bulletsSpawned >= TotalBullets && age > 2)
                this.Destroy();
        }
    }
    public class Thing_DoyouSpearProjectile : Thing
    {
        private Pawn caster;
        private bool isFalling = false;
        private float heightOffset = 0f;
        private float maxHeight;
        private float speed = 0.45f;

        public void Setup(Pawn caster)
        {
            this.caster = caster;
            this.maxHeight = Rand.Range(15f, 25f);
        }

        protected override void Tick()
        {
            if (!isFalling)
            {
                heightOffset += speed;
                if (heightOffset >= maxHeight) isFalling = true;
            }
            else
            {
                heightOffset -= speed * 1.5f;

                if (Find.TickManager.TicksGame % 2 == 0)
                {
                    Thing_DoyouSpearTrail trail = (Thing_DoyouSpearTrail)ThingMaker.MakeThing(ThingDef.Named("Mote_DoyouSpearTrail"));
                    trail.exactPosition = this.DrawPos + new Vector3(Rand.Range(-0.4f, 0.4f), 0, heightOffset + Rand.Range(-0.6f, 0.6f));
                    GenSpawn.Spawn(trail, this.Position, this.Map);
                }

                if (heightOffset <= 0f)
                {
                    Impact();
                }
            }
        }

        private void Impact()
        {
            Map map = this.Map;
            IntVec3 pos = this.Position;

            GenExplosion.DoExplosion(
                pos,
                map,
                2f,
                DamageDefOf.Bomb,
                caster,
                35,
                doSoundEffects: false,      
                screenShakeFactor: 0f,      
                doVisualEffects: false      
            );

            SoundDef.Named("DoyouSpearhit")?.PlayOneShot(new TargetInfo(pos, map));

            Thing_DoyouSpearImpactAnim anim =
                (Thing_DoyouSpearImpactAnim)ThingMaker.MakeThing(
                    ThingDef.Named("Mote_DoyouSpearImpactAnim"));

            anim.exactPosition = pos.ToVector3Shifted();
            GenSpawn.Spawn(anim, pos, map);

            for (int i = 0; i < 4; i++)
            {
                Thing_DoyouSpearDebris debris =
                    (Thing_DoyouSpearDebris)ThingMaker.MakeThing(
                        ThingDef.Named("Mote_DoyouSpearDebris"));

                debris.Initialize(pos.ToVector3Shifted());
                GenSpawn.Spawn(debris, pos, map);
            }

            this.Destroy();
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 visualPos = drawLoc;
            visualPos.z += heightOffset;
            visualPos.y = AltitudeLayer.Projectile.AltitudeFor();

            float angle = isFalling ? 180f : 0f;
            Matrix4x4 matrix = Matrix4x4.TRS(visualPos, Quaternion.Euler(0, angle, 0), new Vector3(2f, 1f, 4f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, DoyouSpearGraphics.BulletMat, 0);
        }
    }

    public class Thing_DoyouSpearFog : Thing
    {
        private int age = 0;
        private const int MaxAge = 60;
        public Vector3 exactPosition;
        private Vector2 velocity;
        public float scale = 1.8f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            velocity = new Vector2(Rand.Range(-0.03f, 0.03f), Rand.Range(-0.03f, 0.03f));
            scale = Rand.Range(1.5f, 2.5f);
        }

        protected override void Tick()
        {
            age++;
            exactPosition.x += velocity.x;
            exactPosition.z += velocity.y;
            scale += 0.04f;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / MaxAge);
            Material mat = FadedMaterialPool.FadedVersionOf(DoyouSpearGraphics.FogMat, alpha);
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_DoyouSpearTrail : Thing
    {
        private int age = 0;
        private const int MaxAge = 25;
        public Vector3 exactPosition;
        private float rotation;
        private float spinRate;
        private float scale;
        private Vector2 velocity;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            rotation = Rand.Range(0f, 360f);
            spinRate = Rand.Range(-6f, 6f);
            scale = Rand.Range(0.4f, 0.8f);
            velocity = new Vector2(Rand.Range(-0.03f, 0.03f), Rand.Range(-0.06f, -0.02f));
        }

        protected override void Tick()
        {
            age++;
            exactPosition.x += velocity.x;
            exactPosition.z += velocity.y;
            rotation += spinRate;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / MaxAge);
            Material mat = FadedMaterialPool.FadedVersionOf(DoyouSpearGraphics.TrailMat, alpha);
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.Projectile.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, rotation, 0), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_DoyouSpearImpactAnim : Thing
    {
        private int age = 0;
        private const int TicksPerFrame = 1;
        public Vector3 exactPosition;

        protected override void Tick()
        {
            age++;
            if (age >= 20 * TicksPerFrame) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Clamp(age / TicksPerFrame, 0, 19);
            Material mat = DoyouSpearGraphics.ImpactAnimMats[frame];
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(2f, 1f, 4f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    public class Thing_DoyouSpearDebris : Thing
    {
        private int age = 0;
        private const int MaxAge = 40;
        public Vector3 exactPosition;
        private Vector3 velocity;
        private float scale;

        public void Initialize(Vector3 startPos)
        {
            exactPosition = startPos;
            float dirX = Rand.Range(0.05f, 0.15f) * (Rand.Bool ? 1 : -1);
            float dirZ = Rand.Range(0.1f, 0.2f);
            velocity = new Vector3(dirX, 0, dirZ);
            scale = Rand.Range(0.3f, 0.9f);
        }

        protected override void Tick()
        {
            age++;
            velocity.z -= 0.015f;
            exactPosition += velocity;
            if (age >= MaxAge) this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / MaxAge);
            Material mat = FadedMaterialPool.FadedVersionOf(DoyouSpearGraphics.DebrisMat, alpha);
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}