using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace merissu
{
    public class GameCondition_ForceWeather_Ability : GameCondition_ForceWeather
    {
        private const string FlyingHediffDefName = "HighClearSky_Flying";
        private HediffDef flyingHediff;

        private HediffDef FlyingHediff
        {
            get
            {
                if (flyingHediff == null)
                    flyingHediff = HediffDef.Named(FlyingHediffDefName);
                return flyingHediff;
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
            if (map == null || FlyingHediff == null) return;

            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned.ToList();

            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            List<Pawn> sortedEnemies = allPawns
                .Where(p => p != null && p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer) && !p.Dead && p.RaceProps.Humanlike)
                .OrderByDescending(p => p.MarketValue)
                .ToList();

            HashSet<Pawn> targetEnemies = new HashSet<Pawn>();
            int enemyQuota = Math.Min(colonistCount, sortedEnemies.Count);
            for (int i = 0; i < enemyQuota; i++)
            {
                targetEnemies.Add(sortedEnemies[i]);
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
                    shouldHaveHediff = true;
                }
                else if (targetEnemies.Contains(pawn))
                {
                    shouldHaveHediff = true;
                }

                Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(FlyingHediff);
                if (shouldHaveHediff)
                {
                    if (existingHediff == null)
                    {
                        pawn.health.AddHediff(FlyingHediff);
                    }
                }
                else
                {
                    if (existingHediff != null)
                    {
                        pawn.health.RemoveHediff(existingHediff);
                    }
                }
            }
        }

        public override void End()
        {
            foreach (Map map in AffectedMaps)
            {
                RemoveHediffFromAllPawns(map);
            }

            Map singleMap = SingleMap;
            base.End();

            if (singleMap != null)
            {
                singleMap.weatherManager.TransitionTo(RimWorld.WeatherDefOf.Clear);
            }
        }

        private void RemoveHediffFromAllPawns(Map map)
        {
            if (map == null || FlyingHediff == null) return;

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn == null) continue;

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(FlyingHediff);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }


    public class CompProperties_AbilityRandomWeather : CompProperties_AbilityEffect
    {
        public List<WeatherDef> weatherPool;
        public bool allowSameAsCurrent = false;
        public int forcedDurationTicks = 20000;
        public GameConditionDef forceWeatherConditionDef;

        public CompProperties_AbilityRandomWeather()
        {
            compClass = typeof(CompAbilityEffect_RandomWeather);
        }
    }

    public class CompAbilityEffect_RandomWeather : CompAbilityEffect
    {
        public new CompProperties_AbilityRandomWeather Props
            => (CompProperties_AbilityRandomWeather)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Map map = parent?.pawn?.Map;
            if (map == null || Props.weatherPool == null || Props.weatherPool.Count == 0) return;

            if (Props.forceWeatherConditionDef == null)
            {
                Log.Error("[merissu] forceWeatherConditionDef is null in XML.");
                return;
            }

            List<WeatherDef> candidates = new List<WeatherDef>();
            foreach (var w in Props.weatherPool)
            {
                if (w == null) continue;
                if (!Props.allowSameAsCurrent && w == map.weatherManager.curWeather) continue;
                candidates.Add(w);
            }

            if (candidates.Count == 0) return;

            WeatherDef chosen = candidates.RandomElement();
            GameCondition_ForceWeather cond = (GameCondition_ForceWeather)GameConditionMaker.MakeCondition(
                Props.forceWeatherConditionDef,
                Props.forcedDurationTicks
            );

            cond.weather = chosen;
            map.gameConditionManager.RegisterCondition(cond);
        }
    }
}