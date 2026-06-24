using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_GateDemonRealm : Ability
    {
        public Ability_GateDemonRealm() : base() { }
        public Ability_GateDemonRealm(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason)) return true;

            if (pawn?.health?.hediffSet != null)
            {
                Hediff powerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
                if (powerHediff == null || powerHediff.Severity < 1f)
                {
                    reason = "符卡不足";
                    return true;
                }
            }

            reason = null;
            return false;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn == null || pawn.Map == null)
                return false;

            Hediff powerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (powerHediff == null || powerHediff.Severity < 1f)
            {
                Messages.Message("符卡不足", pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            powerHediff.Severity -= 1f;

            Map map = pawn.Map;
            IntVec3 targetCell = target.Cell;

            GateDemonHelper.SpawnSmokeBurst(
                targetCell.ToVector3Shifted(),
                map,
                80,
                3f
            );
            ThingDef toriiDef = DefDatabase<ThingDef>.GetNamed("MamizouTorii");
            MamizouTorii torii = (MamizouTorii)ThingMaker.MakeThing(toriiDef);
            GenSpawn.Spawn(torii, targetCell, map);

            torii.casterFaction = pawn.Faction;

            return true;
        }
    }
    public class MamizouTorii : Thing
    {
        public Faction casterFaction;
        private int ageTicks = 0;

        private const int TotalSummonDuration = 180; 
        private const int SmokeInterval = 6;         
        private const int SpawnInterval = 5;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("GateDemonRealm");
                soundDef?.PlayOneShot(new TargetInfo(Position, map));
            }
        }

        protected override void Tick()
        {
            if (Destroyed) return;
            base.Tick();

            ageTicks++;
            Map map = Map;

            if (map != null)
            {
                if (ageTicks % SmokeInterval == 0)
                {
                    Vector3 toriiBottomPos =
                        DrawPos +
                        new Vector3(
                            Rand.Range(-0.4f, 0.4f),
                            0f,
                            -1.2f
                        );
                    GateDemonHelper.SpawnSingleSmoke(toriiBottomPos, map);
                }

                if (ageTicks % SpawnInterval == 0)
                {
                    SummonMonster(map);
                }
            }

            if (ageTicks >= TotalSummonDuration)
            {
                if (map != null)
                {
                    GateDemonHelper.SpawnSmokeBurst(DrawPos, map, 40);
                }
                Destroy(DestroyMode.Vanish);
            }
        }
        private IntVec3 GetSpawnCell(Map map)
        {
            List<IntVec3> cells = new List<IntVec3>();

            for (int x = -1; x <= 1; x++)
            {
                IntVec3 c = Position + new IntVec3(x, 0, -2);

                if (c.InBounds(map) &&
                    c.Walkable(map))
                {
                    cells.Add(c);
                }
            }

            if (cells.Count > 0)
                return cells.RandomElement();

            return Position;
        }
        private void SummonMonster(Map map)
        {
            List<string> kindCandidates = new List<string>
            {
                "Bulbfreak",
                "Chimera",
                "Dreadmeld",
                "Fingerspike",
                "Metalhorror",
                "Noctol",
                "Revenant",
                "ShamblerSwarmers",
                "Sightstealer",
                "Toughspike",
                "Tripspike",
            };


            List<PawnKindDef> availableKinds = new List<PawnKindDef>();
            foreach (string name in kindCandidates)
            {
                PawnKindDef pk = DefDatabase<PawnKindDef>.GetNamedSilentFail(name);
                if (pk != null) availableKinds.Add(pk);
            }

            if (availableKinds.Count == 0)
            {
                PawnKindDef fallback = DefDatabase<PawnKindDef>.GetNamedSilentFail("Warg");
                if (fallback != null) availableKinds.Add(fallback);
            }

            if (availableKinds.Count == 0) return;

            PawnKindDef chosenKind = availableKinds.RandomElement();
            Faction factionToUse = casterFaction ?? Faction.OfPlayer;

            Pawn monster = PawnGenerator.GeneratePawn(chosenKind, factionToUse);

            if (monster.Faction != factionToUse)
            {
                monster.SetFaction(factionToUse);
            }

            IntVec3 spawnCell = GetSpawnCell(map);
            GenSpawn.Spawn(monster, spawnCell, map);

            GateDemonRealmManager manager = Current.Game.GetComponent<GateDemonRealmManager>();
            manager?.RegisterMonster(monster, 1800);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_References.Look(ref casterFaction, "casterFaction");
        }
    }

    public class SummonedMonsterData : IExposable
    {
        public Pawn pawn;
        public int ticksRemaining;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining");
        }
    }

    public class GateDemonRealmManager : GameComponent
    {
        private List<SummonedMonsterData> trackedMonsters = new List<SummonedMonsterData>();

        public GateDemonRealmManager(Game game) { }

        public void RegisterMonster(Pawn monster, int duration)
        {
            if (monster == null) return;
            trackedMonsters.Add(new SummonedMonsterData
            {
                pawn = monster,
                ticksRemaining = duration
            });
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            for (int i = trackedMonsters.Count - 1; i >= 0; i--)
            {
                SummonedMonsterData data = trackedMonsters[i];

                if (data.pawn == null)
                {
                    trackedMonsters.RemoveAt(i);
                    continue;
                }

                if (data.pawn.Dead)
                {
                    Corpse corpse = data.pawn.Corpse;
                    Map corpseMap = corpse?.Map ?? data.pawn.Map;
                    Vector3 pos = corpse?.DrawPos ?? data.pawn.DrawPos;

                    if (corpseMap != null)
                    {
                        GateDemonHelper.SpawnSmokeBurst(pos, corpseMap, 35);
                        corpse?.Destroy(DestroyMode.Vanish);
                    }

                    trackedMonsters.RemoveAt(i);
                    continue;
                }

                if (!data.pawn.Spawned)
                {
                    trackedMonsters.RemoveAt(i);
                    continue;
                }

                data.ticksRemaining--;

                if (data.ticksRemaining <= 0)
                {
                    Map map = data.pawn.Map;
                    if (map != null)
                    {
                        GateDemonHelper.SpawnSmokeBurst(data.pawn.DrawPos, map, 35);
                        data.pawn.Destroy(DestroyMode.Vanish);
                    }

                    trackedMonsters.RemoveAt(i);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref trackedMonsters, "trackedMonsters", LookMode.Deep);
            if (trackedMonsters == null) trackedMonsters = new List<SummonedMonsterData>();
        }
    }

    public static class GateDemonHelper
    {
        private static readonly string[] SmokeDefs =
        {
            "Mote_MamizouHitSmokeA",
            "Mote_MamizouHitSmokeB",
            "Mote_MamizouHitSmokeC",
            "Mote_MamizouHitSmokeD"
        };
        public static void SpawnSmokeBurst(Vector3 center, Map map, int count, float radius = 2.5f)
        {
            if (map == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Rand.InsideUnitCircle * radius;

                Vector3 pos = center +
                    new Vector3(offset.x, 0f, offset.y);

                string randomSmoke = SmokeDefs.RandomElement();

                Mote_MamizouHitSmoke mote =
                    (Mote_MamizouHitSmoke)ThingMaker.MakeThing(
                        ThingDef.Named(randomSmoke));

                GenSpawn.Spawn(mote, pos.ToIntVec3(), map);

                mote.Init(pos);
            }
        }
        public static void SpawnSingleSmoke(Vector3 exactPos, Map map)
        {
            if (map == null) return;
            string randomSmoke = SmokeDefs.RandomElement();
            Mote_MamizouHitSmoke mote = (Mote_MamizouHitSmoke)ThingMaker.MakeThing(ThingDef.Named(randomSmoke));
            GenSpawn.Spawn(mote, exactPos.ToIntVec3(), map);
            mote.Init(exactPos);
        }
    }
}