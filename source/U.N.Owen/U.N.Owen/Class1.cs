using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using static UnityEngine.GraphicsBuffer;

namespace U.N.Owen
{
    [DefOf]
    public static class HediffDefOf
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }

        // Token: 0x04000001 RID: 1
        public static HediffDef FullPower;
    }
    [DefOf]
    public static class FleckDefOf
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        static FleckDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FleckDefOf));
        }

        // Token: 0x04000001 RID: 1
        public static FleckDef MRS_Tail;
    }
    [DefOf]
    public static class DamageDefOf
    {
        static DamageDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DamageDefOf));
        }
        public static DamageDef DanmakuDamage;
    }
    [DefOf]
    public static class ThingDefOf
    {
        static ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
        }
        public static ThingDef PowerPoint;
        public static ThingDef SmallShoot;
        public static ThingDef JunmaiDaiGinjoShu;
        public static ThingDef TH_Meteorolite;
        public static ThingDef TH_Meteor;
        public static ThingDef TH_Comet;
        public static ThingDef TH_Comet_b;
        public static ThingDef TH_Comet_c;
        public static ThingDef GunMarisa;
    }

    public class CompProperties_EquippableAbilityWithDifferentMode : CompProperties_EquippableAbility
    {
        // 三档贴图路径（Resources 路径，不带扩展名）
        public List<string> manaLevelGraphicPaths = new List<string>();

        // 每隔多少 tick 降 1 级 mana
        public int manaDecayIntervalTicks = 12000;

        public CompProperties_EquippableAbilityWithDifferentMode()
        {
            compClass = typeof(CompEquippableAbilityWithDifferentMode);
        }
    }

    public class CompEquippableAbilityWithDifferentMode : CompEquippableAbility
    {
        public int ShootMode = 0; // 0~2
        public int ManaLevel = 0; // 0~2
        private int nextManaDecayTick = -1;

        private Graphic[] cachedGraphics;

        public new CompProperties_EquippableAbilityWithDifferentMode Props =>
            (CompProperties_EquippableAbilityWithDifferentMode)props;


        public Graphic CurrentGraphic
        {
            get
            {
                if (Props.manaLevelGraphicPaths == null)
                    return parent.DefaultGraphic;

                if (cachedGraphics == null || cachedGraphics.Length != 3)
                    cachedGraphics = new Graphic[3];

                int idx = Mathf.Clamp(ManaLevel, 0, 2);
                if (cachedGraphics[idx] == null)
                {
                    string path = Props.manaLevelGraphicPaths[idx];
                    if (path.NullOrEmpty()) return parent.DefaultGraphic;

                    Vector2 drawSize = parent.def.graphicData?.drawSize ?? Vector2.one;
                    cachedGraphics[idx] = GraphicDatabase.Get<Graphic_Single>(
                        path,
                        ShaderDatabase.Cutout,
                        drawSize,
                        parent.DrawColor,
                        parent.DrawColorTwo
                    );
                }

                return cachedGraphics[idx] ?? parent.DefaultGraphic;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            ShootMode = ShootMode;
            ManaLevel = ManaLevel;
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            NotifyGraphicChanged();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (Props.manaDecayIntervalTicks <= 0) return;
            if (Find.TickManager == null) return;

            if (nextManaDecayTick < 0)
                nextManaDecayTick = Find.TickManager.TicksGame + Props.manaDecayIntervalTicks;

            if (Find.TickManager.TicksGame >= nextManaDecayTick)
            {
                if (ManaLevel > 0)
                    ManaLevel -= 1;

                nextManaDecayTick = Find.TickManager.TicksGame + Props.manaDecayIntervalTicks;
            }
        }
        public bool CanGainManaFromFullPower(Pawn pawn, float severityCost, out string reason)
        {
            reason = null;
            if (pawn?.health?.hediffSet == null)
            {
                reason = "灵力不足。";
                return false;
            }

            if (ManaLevel >= 2)
            {
                reason = "已达最大灵力";
                return false;
            }

            HediffDef fullPowerDef = HediffDefOf.FullPower;
            if (fullPowerDef == null)
            {
                reason = "灵力不足。";
                return false;
            }

            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(fullPowerDef);
            if (h == null)
            {
                reason = "灵力不足。";
                return false;
            }

            if (h.Severity < severityCost)
            {
                reason = "灵力不足。";
                return false;
            }

            return true;
        }

        private void NotifyGraphicChanged()
        {
            // 地上物品刷新网格
            if (parent.Spawned && parent.Map != null)
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }

            // 装备在 pawn 身上时，强制刷新 pawn 图形缓存
            if (Holder != null)
            {
                Holder.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ShootMode, "shootMode", 0);
            Scribe_Values.Look(ref ManaLevel, "manaLevel", 0);
            Scribe_Values.Look(ref nextManaDecayTick, "nextManaDecayTick", -1);
        }
    }
    public class ThingWithComps_AbilityModeWeapon : ThingWithComps
    {
        public override Graphic Graphic
        {
            get
            {
                var comp = GetComp<CompEquippableAbilityWithDifferentMode>();
                if (comp != null)
                {
                    var g = comp.CurrentGraphic;
                    if (g != null) return g;
                }
                return base.Graphic;
            }
        }
    }
    public class Verb_SpawnDanmaku_GunMarisa : Verb_LaunchProjectile
    {
        protected override int ShotsPerBurst
        {
            get
            {
                if (shootMode == 0)
                {
                    return manaLevel * 2 + 5;
                }

                return base.BurstShotCount; // 兜底
            }
        }

        public virtual int shootMode
        {
            get
            {
                var comp = base.EquipmentSource?.TryGetComp<CompEquippableAbilityWithDifferentMode>();
                return comp?.ShootMode ?? 0;
            }
        }
        public virtual int manaLevel
        {
            get
            {
                var comp = base.EquipmentSource?.TryGetComp<CompEquippableAbilityWithDifferentMode>();
                return comp?.ManaLevel ?? 0;
            }
        }

        public override ThingDef Projectile
        {
            get
            {
                ThingDef p = base.Projectile;

                if (shootMode == 3)
                {
                    // 只有当 TH_Meteorolite 真的是 projectile Def 时才替换
                    if (ThingDefOf.TH_Meteorolite != null && ThingDefOf.TH_Meteorolite.projectile != null)
                        p = ThingDefOf.TH_Meteorolite;
                }

                // 最终兜底
                if (p == null || p.projectile == null)
                    return null; // 让 Vanilla 走“无投射物”分支，而不是 NRE

                return p;
            }
        }
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            var p = Projectile;
            if (p?.projectile == null) return 0f;

            float r = p.projectile.explosionRadius + p.projectile.explosionRadiusDisplayPadding;
            float miss = verbProps.ForcedMissRadius;
            if (miss > 0f && base.BurstShotCount > 1) r += miss;
            return r;
        }

        protected override bool TryCastShot()
        {
            // 保留原本的跨地图保护
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            switch (shootMode)
            {
                case 1:
                    return SpawnDanmaku(ThingDefOf.TH_Meteorolite, 1);

                case 2:
                    return SpawnDanmaku(ThingDefOf.TH_Comet, 6 + 3 * manaLevel);
                case 3:
                    return SpawnDanmaku(ThingDefOf.TH_Meteor, 1);

                case 0:
                    return SpawnDanmaku(ThingDefOf.TH_Comet_c, 1);

                default:
                    return base.TryCastShot();
            }
        }


        private bool SpawnDanmaku(ThingDef projectileDef, int count)
        {
            if (projectileDef == null || caster?.Map == null)
                return false;

            for (int i = 0; i < count; i++)
            {
                Thing danmaku = ThingMaker.MakeThing(projectileDef);
                var src = danmaku.TryGetComp<CompDanmakuSourceAdvanced>();
                var src2 = danmaku.TryGetComp<CompDanmakuProjectile>()?.Props;
                if (src != null)
                {
                    src.target = currentTarget.Cell;
                    src.core = currentTarget.Cell;
                    src.root = caster.Position;
                    src.angle = -caster.Position.ToVector2().AngleTo(currentTarget.Cell.ToVector2());
                    src.pawn = caster as Pawn;
                    src.Damage = src2.damageAmount + src2.damageAmount * 0.5f * manaLevel;
                    src.Amount = src2.splitCount * 0.5f * manaLevel + src2.splitCount;
                    src.Radius = 5f + 2.5f * manaLevel;
                }

                GenSpawn.Spawn(danmaku, caster.Position, caster.Map);
            }

            return true;
        }
    }
    public class CompProperties_AbilityEffect_CycleShootMode : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityEffect_CycleShootMode()
        {
            compClass = typeof(CompAbilityEffect_CycleShootMode);
        }
    }

    public class CompAbilityEffect_CycleShootMode : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            this.ApplyInner(this.parent.pawn, target.Pawn);
        }

        // Token: 0x06000004 RID: 4 RVA: 0x0000213C File Offset: 0x0000033C
        protected void ApplyInner(Pawn target, Pawn other)
        {
            foreach (ThingWithComps thing in target.equipment.AllEquipmentListForReading)
            {
                if (thing.def == ThingDefOf.GunMarisa)
                {
                    thing.TryGetComp<CompEquippableAbilityWithDifferentMode>().ShootMode += 1;
                    if (thing.TryGetComp<CompEquippableAbilityWithDifferentMode>().ShootMode > 3)
                    {
                        thing.TryGetComp<CompEquippableAbilityWithDifferentMode>().ShootMode = 0;
                    }
                }
            }
        }
    }

    public class CompProperties_AbilityEffect_UseFullPowerGainMana : CompProperties_AbilityEffect
    {
        public float severityCost = 1f;
        public int manaGain = 1;

        public CompProperties_AbilityEffect_UseFullPowerGainMana()
        {
            compClass = typeof(CompAbilityEffect_UseFullPowerGainMana);
        }
    }

    public class CompAbilityEffect_UseFullPowerGainMana : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_UseFullPowerGainMana Props =>
            (CompProperties_AbilityEffect_UseFullPowerGainMana)props;

        public override bool GizmoDisabled(out string reason)
        {
            foreach (ThingWithComps thing in this.parent.pawn.equipment.AllEquipmentListForReading)
            {
                if (thing.def == ThingDefOf.GunMarisa)
                {
                    if (!thing.TryGetComp<CompEquippableAbilityWithDifferentMode>().CanGainManaFromFullPower(parent.pawn, Props.severityCost, out reason))
                    { return true; }   
                }
            }
            reason = null;
            return false;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            this.ApplyInner(this.parent.pawn, target.Pawn);
        }

        // Token: 0x06000004 RID: 4 RVA: 0x0000213C File Offset: 0x0000033C
        protected void ApplyInner(Pawn target, Pawn other)
        {
            foreach (ThingWithComps thing in target.equipment.AllEquipmentListForReading)
            {
                if (thing.def == ThingDefOf.GunMarisa)
                thing.TryGetComp<CompEquippableAbilityWithDifferentMode>().ManaLevel += 1;
                Hediff h = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FullPower);
                h.Severity -= 1.00f;
            }
        }
    } 
    public class Verb_SpawnDanmaku_Kedama : Verb_LaunchProjectile
    {
        private const int PreciseShotCount = 4;
        private const int PreciseIntervalTicks = 8; // “一小段时间”，可调
        private const float SpreadStepAngle = 30f;  // 相邻弹幕 30°

        private enum ShootMode
        {
            Empty = 0,
            Spread3 = 1,
            Precise4 = 2
        }

        protected override bool TryCastShot()
        {
            // 基础合法性检查
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            if (!TryFindShootLineFromTo(caster.Position, currentTarget, out ShootLine shootLine, false) &&
                verbProps.stopBurstWithoutLos)
                return false;

            NotifyEquipmentUsed();
            lastShotTick = Find.TickManager.TicksGame;

            Pawn shooterPawn = GetRealShooterPawn();
            IntVec3 root = caster.Position;
            IntVec3 target = currentTarget.Cell;

            // 每次开火都重新随机
            ShootMode mode = (ShootMode)Rand.RangeInclusive(0, 2);

            switch (mode)
            {
                case ShootMode.Empty:
                    // 空发：消耗这次开火，但不生成弹幕
                    return true;

                case ShootMode.Spread3:
                    SpawnSpread3(root, target, shooterPawn, caster.Map);
                    return true;

                case ShootMode.Precise4:
                    SchedulePrecise4(root, target, shooterPawn, caster.Map);
                    return true;
            }

            return true;
        }

        private void NotifyEquipmentUsed()
        {
            if (EquipmentSource == null) return;

            EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
        }

        private Pawn GetRealShooterPawn()
        {
            Thing shooter = caster;
            CompMannable mannable = caster.TryGetComp<CompMannable>();
            if (mannable?.ManningPawn != null)
                shooter = mannable.ManningPawn;

            return shooter as Pawn;
        }

        private void SpawnSpread3(IntVec3 root, IntVec3 target, Pawn pawn, Map map)
        {
            // 相邻间隔 30° => -30, 0, +30
            SpawnDanmaku(root, target, pawn, map, -SpreadStepAngle);
            SpawnDanmaku(root, target, pawn, map, 0f);
            SpawnDanmaku(root, target, pawn, map, +SpreadStepAngle);
        }

        private void SchedulePrecise4(IntVec3 root, IntVec3 target, Pawn pawn, Map map)
        {
            var scheduler = map.GetComponent<MapComponent_DanmakuScheduler>();
            if (scheduler == null)
            {
                // 兜底：调度器拿不到就立即打4发，避免丢失功能
                for (int i = 0; i < PreciseShotCount; i++)
                    SpawnDanmaku(root, target, pawn, map, 0f);
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < PreciseShotCount; i++)
            {
                scheduler.Schedule(root, target, pawn, now + i * PreciseIntervalTicks);
            }
        }

        public static void SpawnDanmaku(IntVec3 root, IntVec3 target, Pawn pawn, Map map, float angleOffset)
        {
            Thing danmaku = ThingMaker.MakeThing(ThingDefOf.SmallShoot);
            var comp = danmaku.TryGetComp<CompDanmakuSource>();
            if (comp != null)
            {
                comp.core = target;
                comp.root = root;
                comp.angle = -root.ToVector2().AngleTo(target.ToVector2()) + angleOffset;
                comp.pawn = pawn;
            }

            GenSpawn.Spawn(danmaku, root, map);
        }
    }
}


