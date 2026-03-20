using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace U.N.Owen
{
    public class CompProperties_DanmakuSource : CompProperties
    {
        public CompProperties_DanmakuSource()
        {
            compClass = typeof(CompDanmakuSource);
        }
    }

    public class CompDanmakuSource : ThingComp
    {
        public IntVec3 root;
        public IntVec3 core;
        public Vector2 realPosition;
        public float angle;
        public float maxDistance;
        public Pawn pawn;
        public int tick;
        public LocalTargetInfo target;
    }
    public class CompProperties_DanmakuSourceAdvanced : CompProperties_DanmakuSource
    {
        public CompProperties_DanmakuSourceAdvanced()
        {
            this.compClass = typeof(CompDanmakuSourceAdvanced);
        }
    }

    public class CompDanmakuSourceAdvanced : CompDanmakuSource
    {
        public float Damage;  // 覆盖伤害
        public float Amount;  // 覆盖分裂数量
        public float Radius;  // 爆炸半径
    }

    public class CompProperties_DanmakuProjectile : CompProperties
    {
        // 运动
        public float startSpeedMax = 0.8f;
        public float startSpeedMin = 0.5f;
        public float speedAccelerationPerTick = 0f;
        public float maxSpeed = 0.8f;
        public float maxDistance = 12f;
        public float angleOffsetRange = 15;

        // 伤害
        public DamageDef damageDef = RimWorld.DamageDefOf.Bullet;
        public int damageAmount = 10;
        public bool destroyOnPawnHit = true;
        public float downedPawnHitChance = 0.1f;

        // 穿透
        public bool passThroughWalls = false;      // 墙/不可通行建筑
        public bool passThroughBarriers = false;   // 物品障碍(fillPercent)

        // 拖尾
        public bool useMoteTrail = true;
        public bool useLineTrail = true;
        public List<FleckDef> LineTrailDefs;
        public List<ThingDef> trailMoteDefs;
        public float lineTrailWidth = 0.3f;
        public float trailMoteScaleMin = 0.2f;
        public float trailMoteScaleMax = 0.6f;

        // 末端分裂
        public bool splitOnEnd = false;
        public ThingDef splitProjectileDef;
        public int splitCount = 0;
        public float splitAngleOffsetRange = 15;

        // 绘制
        public string materialPath = "Weapons/Bullet/Bullet_MagicBlast";
        public Vector2 drawMeshSize = new Vector2(1f, 1f);
        public float drawY = 15f;
        public float rotationPerTick = 20f;
        public int renderLayer = 5;

        public CompProperties_DanmakuProjectile()
        {
            compClass = typeof(CompDanmakuProjectile);
        }
    }

    public class CompDanmakuProjectile : ThingComp
    {
        public CompProperties_DanmakuProjectile Props => (CompProperties_DanmakuProjectile)props;
    }

    [StaticConstructorOnStartup]
    public class DanmakuProjectileBase : ThingWithComps
    {
        private Vector2 realPosition;
        private Vector2 root;
        private Vector2 core;

        private float speed;
        private float traveled;
        private float baseAngle;
        private float randomAngleOffset;
        private float rotationAngle;

        private IntVec3 lastIntPos;
        private List<Pawn> hitPawns = new List<Pawn>();

        public CompProperties_DanmakuProjectile Cfg => GetComp<CompDanmakuProjectile>()?.Props;
        private CompDanmakuSource Source => GetComp<CompDanmakuSource>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref realPosition, "realPosition");
            Scribe_Values.Look(ref root, "root");
            Scribe_Values.Look(ref core, "core");
            Scribe_Values.Look(ref speed, "speed", 0f);
            Scribe_Values.Look(ref traveled, "traveled", 0f);
            Scribe_Values.Look(ref baseAngle, "baseAngle", 0f);
            Scribe_Values.Look(ref randomAngleOffset, "randomAngleOffset", 0f);
            Scribe_Values.Look(ref rotationAngle, "rotationAngle", 0f);
            Scribe_Values.Look(ref lastIntPos, "lastIntPos");
            Scribe_Collections.Look(ref hitPawns, "hitPawns", LookMode.Reference);
            if (hitPawns == null) hitPawns = new List<Pawn>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (respawningAfterLoad) return;

            if (Cfg == null || Source == null)
            {
                Log.Warning($"{def.defName}: missing CompDanmakuProjectile or CompDanmakuSource.");
                Destroy();
                return;
            }

            root = Source.root.ToVector2();
            core = Source.core.ToVector2();
            baseAngle = Source.angle;
            randomAngleOffset = Rand.Range(-Cfg.angleOffsetRange, Cfg.angleOffsetRange);

            Vector3 v = Position.ToVector3Shifted();
            realPosition = new Vector2(v.x, v.z);

            speed = Rand.Range(Cfg.startSpeedMin, Cfg.startSpeedMax);
            traveled = Vector2.Distance(realPosition, root);
            lastIntPos = Position;

            if (Source.pawn != null) hitPawns.Add(Source.pawn);
        }

        protected override void Tick()
        {
            if (!Spawned || Cfg == null) return;

            Vector2 oldPos = realPosition;
            speed = Mathf.Min(speed + Cfg.speedAccelerationPerTick, Cfg.maxSpeed);

            float moveAngle = baseAngle + randomAngleOffset;
            float rad = moveAngle * Mathf.Deg2Rad;
            realPosition += new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;

            IntVec3 newCell = new Vector3(realPosition.x, 0f, realPosition.y).ToIntVec3();
            if (!newCell.InBounds(Map))
            {
                Destroy();
                OnExplode(0);
                return;
            }

            Position = newCell;
            traveled += Vector2.Distance(oldPos, realPosition);
            rotationAngle += Cfg.rotationPerTick;
            if (this.Position==Source.target)
            {
                OnExplode(0);
            }
            SpawnTrail(oldPos, realPosition);
            if (HandleCollision(lastIntPos, newCell, moveAngle))
                return;

            lastIntPos = newCell;

            if (traveled >= Cfg.maxDistance)
            {
                DoSplit(moveAngle);
                OnExplode(0);
                Destroy();
            }
        }

        private void SpawnTrail(Vector2 from, Vector2 to)
        {
            if (Cfg.useLineTrail && Cfg.LineTrailDefs != null && Cfg.LineTrailDefs.Count > 0)
            {
                FleckDef fleckDef = Cfg.LineTrailDefs.RandomElement();
                FleckMaker.ConnectingLine(
                    new Vector3(to.x, 0f, to.y),
                    new Vector3(from.x, 0f, from.y),
                    fleckDef,
                    Map,
                    Cfg.lineTrailWidth);
            }

            if (Cfg.useMoteTrail && Cfg.trailMoteDefs != null && Cfg.trailMoteDefs.Count > 0)
            {
                ThingDef moteDef = Cfg.trailMoteDefs.RandomElement();
                MoteThrown mote = ThingMaker.MakeThing(moteDef) as MoteThrown;
                if (mote != null)
                {
                    Vector3 pos = DrawPos;
                    pos.y = AltitudeLayer.Projectile.AltitudeFor();
                    pos.x += Rand.Range(-1f, 1f);
                    pos.z += Rand.Range(-1f, 1f);
                    mote.Scale = Rand.Range(Cfg.trailMoteScaleMin, Cfg.trailMoteScaleMax);
                    mote.rotationRate = 45f;
                    mote.exactPosition = pos;
                    mote.SetVelocity(0f, 0f);
                    GenSpawn.Spawn(mote, Position, Map);
                }
            }
        }

        private bool HandleCollision(IntVec3 from, IntVec3 to, float angle)
        {
            if (from == to) return false;

            foreach (IntVec3 cell in GenSight.BresenhamCellsBetween(from.x, from.z, to.x, to.z))
            {
                foreach (Thing t in cell.GetThingList(Map).ToList())
                {
                    if (t == this) continue;

                    // Pawn
                    if (t is Pawn p)
                    {
                        if (Source?.pawn != null && p == Source.pawn) continue;
                        if (hitPawns.Contains(p)) continue;

                        float hitChance = p.Downed ? Cfg.downedPawnHitChance : 1f;
                        if (Rand.Value <= hitChance)
                        {
                            DealDamage(p, angle);
                            hitPawns.Add(p);
                            if (Cfg.destroyOnPawnHit)
                            {
                                Destroy();
                                OnExplode(0);
                                return true;
                            }
                        }
                        continue;
                    }

                    // Wall / impassable building
                    if (t is Building b && b.def.passability == Traversability.Impassable)
                    {
                        if (!Cfg.passThroughWalls)
                        {
                            DealDamage(b, angle);
                            OnExplode(0);
                            Destroy();
                            return true;
                        }
                        continue;
                    }

                    // Barrier item
                    if (t.def.fillPercent > 0f)
                    {
                        if (!Cfg.passThroughBarriers)
                        {
                            if (UnityEngine.Random.Range(0f,1f) < t.def.fillPercent)
                            {
                                DealDamage(t, angle);
                                OnExplode(0);
                                Destroy();
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void DealDamage(Thing target, float angle)
        {
            DamageDef dd = Cfg.damageDef ?? RimWorld.DamageDefOf.Bullet;
            target.TakeDamage(new DamageInfo(
                dd,
                Cfg.damageAmount,
                0f,
                angle,
                this,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown));
        }

        private void DoSplit(float angle)
        {
            if (!Cfg.splitOnEnd || Cfg.splitProjectileDef == null || Cfg.splitCount <= 0 || Map == null) return;

            for (int i = 0; i < Cfg.splitCount; i++)
            {
                Thing child = ThingMaker.MakeThing(Cfg.splitProjectileDef);
                CompDanmakuSource src = child.TryGetComp<CompDanmakuSource>();
                if (src != null)
                {
                    src.root = Position;
                    src.core = Position;
                    src.pawn = Source?.pawn;
                    src.angle = angle + Rand.Range(-Cfg.splitAngleOffsetRange, Cfg.splitAngleOffsetRange);
                }
                GenSpawn.Spawn(child, Position, Map);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var cfg = Cfg;
            if (cfg == null) return;

            Matrix4x4 matrix = default;
            Quaternion rot = Quaternion.Euler(0f, rotationAngle, 0f);
            matrix.SetTRS(new Vector3(realPosition.x, cfg.drawY, realPosition.y), rot, Vector3.one);

            Material mat = MaterialPool.MatFrom(
                cfg.materialPath,
                ShaderDatabase.Transparent,
                MapMaterialRenderQueues.Tornado);

            Graphics.DrawMesh(
                MeshPool.GridPlane(cfg.drawMeshSize),
                matrix,
                mat,
                0,
                null,
                0,
                null);
        }
        protected CompDanmakuSource SourceComp => GetComp<CompDanmakuSource>();
        protected CompDanmakuSourceAdvanced SourceAdvComp => GetComp<CompDanmakuSourceAdvanced>();
        protected CompProperties_DanmakuProjectile ProjectileCfg => GetComp<CompDanmakuProjectile>()?.Props;

        protected virtual int ResolveDamageAmount()
        {
            return ProjectileCfg?.damageAmount ?? 10;
        }

        protected virtual int ResolveSplitCount()
        {
            return ProjectileCfg?.splitCount ?? 0;
        }

        protected virtual bool ExplodeOnBlocked => false;
        protected virtual bool ExplodeOnReachTarget => false;
        protected virtual bool ExplodeOnMaxDistance => false;

        protected virtual void OnExplode(float moveAngle) { } // 子类可实现爆炸+生成小弹幕
        protected virtual bool ReachedTarget()
        {
            return SourceComp != null && SourceComp.target.IsValid && this.Position == SourceComp.target.Cell;
        }
    }
    public class DanmakuAdvancedScatterProjectile : DanmakuProjectileBase
    {
        protected override int ResolveDamageAmount()
        {
            if (SourceAdvComp != null && SourceAdvComp.Damage > 0f)
                return Mathf.RoundToInt(SourceAdvComp.Damage);

            return base.ResolveDamageAmount();
        }

        protected override int ResolveSplitCount()
        {
            if (SourceAdvComp != null && SourceAdvComp.Amount > 0f)
                return Mathf.Max(0, Mathf.RoundToInt(SourceAdvComp.Amount));

            return base.ResolveSplitCount();
        }
    }


    public class DanmakuGravityExplode : DanmakuProjectileBase
    {
        // 被阻挡 / 到达目标 / 超距离 都触发爆炸
        protected override bool ExplodeOnBlocked => true;
        protected override bool ExplodeOnReachTarget => true;
        protected override bool ExplodeOnMaxDistance => true;

        protected override int ResolveDamageAmount()
        {
            if (SourceAdvComp != null && SourceAdvComp.Damage > 0f)
                return Mathf.RoundToInt(SourceAdvComp.Damage);

            return base.ResolveDamageAmount();
        }

        protected override void OnExplode(float moveAngle)
        {
            if (Map == null) return;

            // 半径取 Advanced 参数
            float radius = 2f;
            if (SourceAdvComp != null && SourceAdvComp.Radius > 0f)
                radius = SourceAdvComp.Radius;

            // 1) 先聚拢 Pawn
            PullPawnsToCenter(radius);

            // 2) 再爆炸（GenExplosion）
            DamageDef dd = ProjectileCfg?.damageDef ?? RimWorld.DamageDefOf.Bomb;
            int dmg = ResolveDamageAmount();

            GenExplosion.DoExplosion(
                center: Position,
                map: Map,
                radius: radius,
                damType: dd,
                instigator: this,
                damAmount: dmg
            );
        }

        private void PullPawnsToCenter(float radius)
        {
            float affectRadius = radius * 2f;   // 2*Radius 范围
            float pullDistance = radius / 2f;   // 向中心移动 Radius/2
            int steps = Mathf.Max(1, Mathf.RoundToInt(pullDistance));

            List<Pawn> pawns = new List<Pawn>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, affectRadius, true))
            {
                if (!cell.InBounds(Map)) continue;
                Pawn p = cell.GetFirstPawn(Map);
                if (p == null || !p.Spawned || p.Dead) continue;
                pawns.Add(p);
            }

            foreach (Pawn p in pawns)
            {
                IntVec3 cur = p.Position;
                for (int i = 0; i < steps; i++)
                {
                    int dx = Position.x - cur.x;
                    int dz = Position.z - cur.z;
                    if (dx == 0 && dz == 0) break;

                    IntVec3 next = new IntVec3(cur.x + Mathf.Clamp(dx, -1, 1), 0, cur.z + Mathf.Clamp(dz, -1, 1));
                    if (!next.InBounds(Map) || !next.Standable(Map))
                        break;

                    // 直接位移（Thing.Position 在源码中是可写属性）
                    p.Position = next;
                    cur = next;
                }
            }
        }

        private void SpawnFiveChildren(float baseAngle)
        {
            ThingDef childDef = ProjectileCfg?.splitProjectileDef;
            if (childDef == null || Map == null) return;

            const int count = 5;
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float a = baseAngle + i * step;
                Thing child = ThingMaker.MakeThing(childDef);

                // 优先写 Advanced，保证子弹也能拿到 Damage/Radius 等
                var adv = child.TryGetComp<CompDanmakuSourceAdvanced>();
                if (adv != null)
                {
                    adv.root = Position;
                    adv.core = Position;
                    adv.angle = a;
                    adv.pawn = SourceComp?.pawn;
                    adv.Damage = SourceAdvComp?.Damage ?? ResolveDamageAmount();
                    adv.Radius = SourceAdvComp?.Radius ?? 1.5f;
                    adv.Amount = 0f;
                }
                else
                {
                    var src = child.TryGetComp<CompDanmakuSource>();
                    if (src != null)
                    {
                        src.root = Position;
                        src.core = Position;
                        src.angle = a;
                        src.pawn = SourceComp?.pawn;
                    }
                }

                GenSpawn.Spawn(child, Position, Map);
            }
        }
    }
}
