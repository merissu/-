using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace merissu
{

    public class WeatherConditionExtension : DefModExtension
    {
        public string hediffDefName;           // 要添加的状态 (Hediff) 的名称
        public bool applyToAllEnemies = false; // 是否对所有敌人生效 (false 则使用殖民者配额)
        public bool playerOnly = false;        // 是否只对玩家生效
        public bool friendlyOnly = false;      // 是否只对友好/中立派系生效
    }


    public class GameCondition_UniversalWeather : GameCondition_ForceWeather
    {
        private HediffDef cachedHediff;
        private WeatherConditionExtension Props => def.GetModExtension<WeatherConditionExtension>();

        private HediffDef TargetHediff
        {
            get
            {
                if (cachedHediff == null && Props != null && !Props.hediffDefName.NullOrEmpty())
                    cachedHediff = HediffDef.Named(Props.hediffDefName);
                return cachedHediff;
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            if (Find.TickManager.TicksGame % 60 == 0) 
            {
                foreach (Map map in AffectedMaps)
                {
                    MaintainHediffOnPawns(map);
                }
            }
        }

        private void MaintainHediffOnPawns(Map map)
        {
            if (map == null || TargetHediff == null || Props == null) return;

            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned.ToList();
            HashSet<Pawn> targetEnemies = new HashSet<Pawn>();

            if (!Props.applyToAllEnemies)
            {
                int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;
                var sortedEnemies = allPawns
                    .Where(p => p != null && p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer) && !p.Dead && p.RaceProps.Humanlike)
                    .OrderByDescending(p => p.MarketValue)
                    .ToList();

                int enemyQuota = Math.Min(colonistCount, sortedEnemies.Count);
                for (int i = 0; i < enemyQuota; i++)
                {
                    targetEnemies.Add(sortedEnemies[i]);
                }
            }

            foreach (Pawn pawn in allPawns)
            {
                if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike) continue;

                bool shouldHaveHediff = false;

                if (pawn.Faction == Faction.OfPlayer)
                {
                    shouldHaveHediff = true;
                }
                else if (pawn.Faction != null && !pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (!Props.playerOnly) shouldHaveHediff = true;
                }
                else if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (!Props.playerOnly && !Props.friendlyOnly)
                    {
                        if (Props.applyToAllEnemies || targetEnemies.Contains(pawn))
                            shouldHaveHediff = true;
                    }
                }

                Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(TargetHediff);
                if (shouldHaveHediff)
                {
                    if (existingHediff == null) pawn.health.AddHediff(TargetHediff);
                }
                else
                {
                    if (existingHediff != null) pawn.health.RemoveHediff(existingHediff);
                }
            }
        }

        public override void End()
        {
            foreach (Map map in AffectedMaps)
            {
                RemoveHediffFromAllPawns(map);
            }
            base.End();

            if (SingleMap != null)
            {
                SingleMap.weatherManager.TransitionTo(RimWorld.WeatherDefOf.Clear);
            }
        }

        private void RemoveHediffFromAllPawns(Map map)
        {
            if (map == null || TargetHediff == null) return;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TargetHediff);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }
    }


    public class CompProperties_AbilityRandomWeather : CompProperties_AbilityEffect
    {
        public List<GameConditionDef> conditionPool; 
        public int forcedDurationTicks = 20000;

        public CompProperties_AbilityRandomWeather()
        {
            compClass = typeof(CompAbilityEffect_RandomWeather);
        }
    }

    public class CompAbilityEffect_RandomWeather : CompAbilityEffect
    {
        public new CompProperties_AbilityRandomWeather Props => (CompProperties_AbilityRandomWeather)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent?.pawn?.Map;
            if (map == null || Props.conditionPool == null || Props.conditionPool.Count == 0) return;

            GameConditionDef chosenDef = Props.conditionPool.RandomElement();
            GameCondition cond = GameConditionMaker.MakeCondition(chosenDef, Props.forcedDurationTicks);

            map.gameConditionManager.RegisterCondition(cond);
            Messages.Message("天候改变为: " + chosenDef.label, MessageTypeDefOf.PositiveEvent);
        }
    }
}