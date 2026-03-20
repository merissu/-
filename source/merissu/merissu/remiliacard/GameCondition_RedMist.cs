using RimWorld;
using Verse;
using System.Collections.Generic;

namespace merissu
{
    public class GameCondition_RedMagicFog : GameCondition_ForceWeather
    {

        private static readonly HediffDef VampireDef =
            HediffDef.Named("vampire");

        private static readonly HediffDef OrganicDebuff =
            HediffDef.Named("RedMagicEnemyOrganic");

        private static readonly HediffDef MechDebuff =
            HediffDef.Named("RedMagicEnemyMech");

        public override int TransitionTicks => 300;

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            if (Find.TickManager.TicksGame % 1200 == 0)
            {
                ApplyEffects();
            }
        }
        private void ApplyEffects()
        {
            List<Map> maps = AffectedMaps;

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];

                IReadOnlyList<Pawn> pawns =
                    map.mapPawns.AllPawnsSpawned;

                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];

                    if (!pawn.Spawned || pawn.Dead)
                        continue;

                    if (pawn.Position.Roofed(map))
                        continue;

                    if (pawn.Faction == Faction.OfPlayer &&
                        pawn.IsColonist &&
                        pawn.RaceProps.Humanlike)
                    {
                        if (!pawn.health.hediffSet.HasHediff(VampireDef))
                        {
                            Hediff h = pawn.health.AddHediff(VampireDef);
                            h.Severity = VampireDef.maxSeverity; 
                        }
                        continue;
                    }

                    if (pawn.Faction != Faction.OfPlayer)
                    {
                        if (pawn.RaceProps.IsMechanoid)
                        {
                            if (!pawn.health.hediffSet.HasHediff(MechDebuff))
                            {
                                pawn.health.AddHediff(MechDebuff);
                            }
                        }
                        else
                        {
                            if (!pawn.health.hediffSet.HasHediff(OrganicDebuff))
                            {
                                pawn.health.AddHediff(OrganicDebuff);
                            }
                        }
                    }
                }
            }
        }

        public override void End()
        {
            base.End();

            List<Map> maps = AffectedMaps;

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];

                IReadOnlyList<Pawn> pawns =
                    map.mapPawns.AllPawnsSpawned;

                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];

                    if (pawn.Dead || pawn.health == null)
                        continue;

                    RemoveIfExists(pawn, OrganicDebuff);
                    RemoveIfExists(pawn, MechDebuff);
                }
            }

            if (SingleMap != null)
                SingleMap.weatherDecider.StartNextWeather();
        }

        private void RemoveIfExists(Pawn pawn, HediffDef def)
        {
            Hediff h = pawn.health.hediffSet
                .GetFirstHediffOfDef(def);

            if (h != null)
                pawn.health.RemoveHediff(h);
        }
    }
}