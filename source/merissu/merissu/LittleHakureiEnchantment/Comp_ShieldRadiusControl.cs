using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Comp_ShieldRadiusControl : ThingComp
    {
        public CompProperties_ShieldRadiusControl Props => (CompProperties_ShieldRadiusControl)props;

        private CompProjectileInterceptor interceptor;
        private CompRefuelable refuelable;

        private bool allowGround;
        private bool allowAir;
        private bool revealStealthEnabled = false;
        private bool touhouDieEnabled = false;

        private int sealedBeadCount = 0;
        private int lastSealTick = -999;

        private const int MaxBeadsLimit = 7;

        private bool? lastGroundState = null;
        private bool? lastAirState = null;

        private static readonly HashSet<ushort> KnownStealthHediffShortIDs = new HashSet<ushort>();
        private static readonly HashSet<ushort> KnownNormalHediffShortIDs = new HashSet<ushort>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            interceptor = parent.GetComp<CompProjectileInterceptor>();
            refuelable = parent.GetComp<CompRefuelable>();

            if (interceptor != null)
            {
                allowGround = interceptor.Props.interceptGroundProjectiles;
                allowAir = interceptor.Props.interceptAirProjectiles;
            }

            UpdateFuelConsumption();
            UpdateInterceptorState(true); 
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref revealStealthEnabled, "revealStealthEnabled", false);
            Scribe_Values.Look(ref touhouDieEnabled, "touhouDieEnabled", false);
            Scribe_Values.Look(ref sealedBeadCount, "sealedBeadCount", 0);
            Scribe_Values.Look(ref lastSealTick, "lastSealTick", -999);
        }

        public override void CompTick()
        {
            base.CompTick();

            Map map = parent.Map;
            if (map == null) return;

            if (parent.IsHashIntervalTick(30))
            {
                UpdateInterceptorState(false);

                if (refuelable != null && refuelable.HasFuel)
                {
                    if (revealStealthEnabled || touhouDieEnabled)
                    {
                        ProcessEnemiesInRadius(map);
                    }
                }
            }

            if (parent.IsHashIntervalTick(250) && refuelable != null && refuelable.HasFuel)
            {
                TickSpiritualPowerRecovery(map);
            }
        }

        private void ProcessEnemiesInRadius(Map map)
        {
            float radius = interceptor != null ? interceptor.Props.radius : 30f;
            IntVec3 center = parent.Position;
            Faction parentFaction = parent.Faction;

            HediffDef dieDef = touhouDieEnabled ? DefDatabase<HediffDef>.GetNamedSilentFail("touhoudie") : null;

            var allPawns = map.mapPawns.AllPawnsSpawned;
            int pawnCount = allPawns.Count;

            for (int i = 0; i < pawnCount; i++)
            {
                Pawn pawn = allPawns[i];

                if (pawn.Dead || pawn.Faction == null || !pawn.HostileTo(parentFaction)) continue;
                if (!pawn.Position.InHorDistOf(center, radius)) continue;

                if (revealStealthEnabled && pawn.health?.hediffSet?.hediffs != null)
                {
                    var hediffs = pawn.health.hediffSet.hediffs;
                    for (int j = hediffs.Count - 1; j >= 0; j--)
                    {
                        Hediff h = hediffs[j];
                        if (h?.def == null) continue;

                        if (IsStealthHediff(h.def))
                        {
                            hediffs.RemoveAt(j);
                            pawn.health.Notify_HediffChanged(h); 
                        }
                    }
                }

                if (touhouDieEnabled && dieDef != null)
                {
                    if (!pawn.health.hediffSet.HasHediff(dieDef))
                    {
                        pawn.health.AddHediff(dieDef);
                    }
                }
            }
        }
        private bool IsStealthHediff(HediffDef def)
        {
            ushort id = def.shortHash;
            if (KnownStealthHediffShortIDs.Contains(id)) return true;
            if (KnownNormalHediffShortIDs.Contains(id)) return false;

            string name = def.defName;
            if (name.Contains("Invisible") || name.Contains("Invisibility") || name.Contains("Stealth"))
            {
                KnownStealthHediffShortIDs.Add(id);
                return true;
            }

            KnownNormalHediffShortIDs.Add(id);
            return false;
        }

        private void TickSpiritualPowerRecovery(Map map)
        {
            if (sealedBeadCount <= 0) return;

            float radius = interceptor != null ? interceptor.Props.radius : 30f;
            IntVec3 center = parent.Position;

            float maxSeverityLimit = 1.0f + (sealedBeadCount - 1) * 0.5f;
            float baseRecoveryPerInterval = 1.0f / (60000f / 250f);

            HediffDef powerDef = Props.spiritualPowerDef ?? DefDatabase<HediffDef>.GetNamedSilentFail("spiritualpower");
            if (powerDef == null) return;

            var colonists = map.mapPawns.FreeColonistsSpawned;
            int colonistCount = colonists.Count;

            for (int i = 0; i < colonistCount; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn.Dead || pawn.Downed) continue;

                if (pawn.Position.InHorDistOf(center, radius))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(powerDef);

                    if (hediff == null)
                    {
                        hediff = pawn.health.AddHediff(powerDef);
                        hediff.Severity = 0.01f;
                    }
                    else
                    {
                        if (hediff.Severity >= maxSeverityLimit) continue;
                        hediff.Severity = Mathf.Min(hediff.Severity + baseRecoveryPerInterval, maxSeverityLimit);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (interceptor == null) yield break;
            float radius = interceptor.Props.radius;

            yield return new Command_Action
            {
                defaultLabel = "缩小结界范围",
                defaultDesc = $"当前半径：{radius:F0}\n每日灵力消耗：{radius:F0}",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                action = () => ChangeRadius(-Props.step)
            };

            yield return new Command_Action
            {
                defaultLabel = "扩大结界范围",
                defaultDesc = $"当前半径：{radius:F0}\n每日灵力消耗：{radius:F0}",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                action = () => ChangeRadius(+Props.step)
            };

            yield return new Command_Toggle
            {
                defaultLabel = "显形结界",
                defaultDesc = "使结界范围内所有正在隐身的敌对生物现身\n每日额外消耗：10 灵力",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                isActive = () => revealStealthEnabled,
                toggleAction = () =>
                {
                    revealStealthEnabled = !revealStealthEnabled;
                    UpdateFuelConsumption();
                }
            };

            yield return new Command_Toggle
            {
                defaultLabel = "必灭结界",
                defaultDesc = "使结界范围内所有敌对单位获得幻想入\n每日额外消耗：10 灵力",
                icon = ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                isActive = () => touhouDieEnabled,
                toggleAction = () =>
                {
                    touhouDieEnabled = !touhouDieEnabled;
                    UpdateFuelConsumption();
                }
            };

            ThingDef beadDef = Props.beadDef ?? DefDatabase<ThingDef>.GetNamedSilentFail("SupernaturalBead");
            float currentLimit = sealedBeadCount > 0 ? (1.0f + (sealedBeadCount - 1) * 0.5f) : 0f;

            string beadDesc = $"当前已封入：{sealedBeadCount} / {MaxBeadsLimit} 颗\n当前提供的灵力上限：{currentLimit:F1}";
            if (sealedBeadCount >= MaxBeadsLimit)
            {
                beadDesc += "\n\n(已达到最大封入上限)";
            }

            yield return new Command_Action
            {
                defaultLabel = "封入灵异珠",
                defaultDesc = beadDesc,
                icon = beadDef != null ? beadDef.uiIcon : ContentFinder<Texture2D>.Get("Other/onmyoBall"),
                disabled = sealedBeadCount >= MaxBeadsLimit,
                disabledReason = "已达到最大封入数量（7颗）",
                action = () => TrySealSupernaturalBead(beadDef)
            };
        }

        private void TrySealSupernaturalBead(ThingDef beadDef)
        {
            if (beadDef == null)
            {
                Messages.Message("未找到灵异珠，无法封入。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (sealedBeadCount >= MaxBeadsLimit)
            {
                Messages.Message("无法封入更多灵异珠。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (Find.TickManager.TicksGame < lastSealTick + 10) return;

            Map map = parent.Map;
            if (map == null) return;

            Thing targetBead = map.listerThings.ThingsOfDef(beadDef)
                .FirstOrDefault(t => t.Spawned && !t.Destroyed && t.stackCount > 0 && !t.Position.Fogged(map));

            if (targetBead != null)
            {
                lastSealTick = Find.TickManager.TicksGame;
                targetBead.SplitOff(1).Destroy();
                sealedBeadCount++;

                SoundStarter.PlayOneShot(SoundDef.Named("powerup"), new TargetInfo(parent.Position, map));
                Messages.Message($"成功将一颗灵异珠封入结界！当前已封入：{sealedBeadCount}/{MaxBeadsLimit}颗。", parent, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("地图上没有可用的灵异珠", MessageTypeDefOf.RejectInput, false);
            }
        }

        private void ChangeRadius(float delta)
        {
            if (interceptor == null) return;
            float current = interceptor.Props.radius;
            float newRadius = Mathf.Clamp(current + delta, Props.minRadius, Props.maxRadius);
            if (Mathf.Approximately(newRadius, current)) return;
            interceptor.Props.radius = newRadius;
            UpdateFuelConsumption();
            UpdateInterceptorState(false);
        }

        private void UpdateFuelConsumption()
        {
            if (refuelable == null || interceptor == null) return;
            float consumption = interceptor.Props.radius;
            if (revealStealthEnabled) consumption += 10f;
            if (touhouDieEnabled) consumption += 10f;
            refuelable.Props.fuelConsumptionRate = consumption;
        }
        private void UpdateInterceptorState(bool forceRefresh)
        {
            if (interceptor == null || refuelable == null) return;

            bool targetGround = refuelable.HasFuel && allowGround;
            bool targetAir = refuelable.HasFuel && allowAir;

            if (forceRefresh || lastGroundState != targetGround || lastAirState != targetAir)
            {
                interceptor.Props.interceptGroundProjectiles = targetGround;
                interceptor.Props.interceptAirProjectiles = targetAir;

                lastGroundState = targetGround;
                lastAirState = targetAir;
            }
        }
    }
}