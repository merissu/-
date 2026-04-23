using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace merissu
{
    public class CustomRaidExtension : DefModExtension
    {
        public HediffDef hediffZayu;
        public HediffDef hediffLow;
        public HediffDef hediffMid;
        public HediffDef hediffHigh;
        public HediffDef hediffBoss;
        public HediffDef hediffExtraF;
        public List<SongDef> songsZayu;
        public List<SongDef> songsLow;
        public List<SongDef> songsMid;
        public List<SongDef> songsHigh;
        public List<SongDef> songsBoss;
        public List<PawnKindDef> poolZayu;
        public List<PawnKindDef> poolLow;
        public List<PawnKindDef> poolMid;
        public List<PawnKindDef> poolHigh;
        public List<PawnKindDef> poolBoss;
    }

    public class SpawnRequest : IExposable
    {
        public PawnKindDef kind;
        public HediffDef specificHediff;

        public SpawnRequest() { }

        public SpawnRequest(PawnKindDef kind, HediffDef hediff)
        {
            this.kind = kind;
            this.specificHediff = hediff;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref kind, "kind");
            Scribe_Defs.Look(ref specificHediff, "specificHediff");
        }
    }

    public class Merissu_RaidManager : MapComponent
    {
        public int unlockedLevel = 1;

        private const int MaxTotalSpawn = 500;
        private const int MaxAliveOnMap = 150;

        private bool raidActive;
        private int raidTotalCount; 
        private List<SpawnRequest> pending = new List<SpawnRequest>();
        private List<Pawn> alive = new List<Pawn>();

        private Faction raidFaction;
        private Lord raidLord;
        private HediffDef extraHediff;
        private bool addExtraHediff;

        private int sideCursor;

        public bool RaidActive => raidActive;
        public int RaidTotalCount => raidTotalCount;
        public int RaidRemainingCount => (pending?.Count ?? 0) + (alive?.Count ?? 0);
        public int RaidAliveCount => alive?.Count ?? 0;
        public Lord RaidLord => raidLord;

        public Merissu_RaidManager(Map map) : base(map) { }

        public void StartRaid(List<SpawnRequest> requests, Faction faction, HediffDef extra, bool addExtra, CustomRaidExtension ext, int level)
        {
            raidActive = true;
            pending = (requests ?? new List<SpawnRequest>()).Take(MaxTotalSpawn).ToList();
            alive.Clear();

            raidTotalCount = pending.Count;
            raidFaction = faction ?? Faction.OfEntities;
            raidLord = null;
            extraHediff = extra;
            addExtraHediff = addExtra;
            sideCursor = Rand.Range(0, 4);

            PlayLevelAppropriateMusic(ext, level);
        }

        private void PlayLevelAppropriateMusic(CustomRaidExtension ext, int level)
        {
            if (ext == null) return;

            List<SongDef> songPool = null;
            switch (level)
            {
                case 1: songPool = ext.songsZayu; break;
                case 2: songPool = ext.songsLow; break;
                case 3: songPool = ext.songsMid; break;
                case 4: songPool = ext.songsHigh; break;
                case 5: songPool = ext.songsBoss; break;
            }

            if (songPool.NullOrEmpty()) return;

            Map currentMap = map ?? Find.CurrentMap;
            if (currentMap == null) return;

            Season currentSeason = GenLocalDate.Season(currentMap);
            float dayPercent = GenLocalDate.DayPercent(currentMap);

            TimeOfDay currentTime = (dayPercent < 0.2f || dayPercent > 0.7f) ? TimeOfDay.Night : TimeOfDay.Day;

            var bestMatch = songPool.Where(s =>
                (s.allowedSeasons.NullOrEmpty() || s.allowedSeasons.Contains(currentSeason)) &&
                (s.allowedTimeOfDay == TimeOfDay.Any || s.allowedTimeOfDay == currentTime)
            ).ToList();

            SongDef chosenSong = null;

            if (!bestMatch.Any())
            {
                var seasonMatch = songPool.Where(s => s.allowedSeasons.NullOrEmpty() || s.allowedSeasons.Contains(currentSeason)).ToList();
                chosenSong = seasonMatch.Count > 0 ? seasonMatch.RandomElement() : songPool.RandomElement();
            }
            else
            {
                chosenSong = bestMatch.RandomElement();
            }

            if (chosenSong != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(chosenSong, false);
            }
        }
        public override void MapComponentTick()
        {
            if (!raidActive) return;

            alive.RemoveAll(p => p == null || p.Destroyed || p.Dead || !p.Spawned || p.Map != map);

            int need = MaxAliveOnMap - alive.Count;
            int canSpawn = Math.Min(need, pending.Count);

            for (int i = 0; i < canSpawn; i++)
            {
                if (!TrySpawnOne()) break;
            }

            if (pending.Count == 0 && alive.Count == 0)
            {
                raidActive = false;
                raidLord = null;
            }
        }

        private bool TrySpawnOne()
        {
            if (pending.Count == 0) return false;

            IntVec3 cell;
            if (!TryFindDistributedEntryCell(out cell)) return false;

            SpawnRequest req = pending[0];
            pending.RemoveAt(0);

            Pawn pawn = PawnGenerator.GeneratePawn(req.kind, raidFaction);
            GenSpawn.Spawn(pawn, cell, map);

            if (req.specificHediff != null)
                pawn.health.AddHediff(req.specificHediff);

            if (addExtraHediff && extraHediff != null)
                pawn.health.AddHediff(extraHediff);

            EnsureLordAndJoin(pawn);
            alive.Add(pawn);
            return true;
        }

        private void EnsureLordAndJoin(Pawn pawn)
        {
            if (raidLord == null || raidLord.Map != map || raidLord.LordJob == null)
            {
                raidLord = LordMaker.MakeNewLord(
                    raidFaction,
                    new LordJob_MerissuAssault(),
                    map,
                    new List<Pawn> { pawn }
                );
            }
            else
            {
                raidLord.AddPawn(pawn);
            }
        }

        private Predicate<IntVec3> MakeSideValidator(int side)
        {
            switch (side)
            {
                case 0: return c => c.z >= map.Size.z - 2;
                case 1: return c => c.x >= map.Size.x - 2;
                case 2: return c => c.z <= 1;
                default: return c => c.x <= 1;
            }
        }

        private bool TryFindDistributedEntryCell(out IntVec3 cell)
        {
            for (int i = 0; i < 4; i++)
            {
                int side = (sideCursor + i) % 4;
                if (RCellFinder.TryFindRandomPawnEntryCell(
                    out cell, map, CellFinder.EdgeRoadChance_Hostile, false, MakeSideValidator(side)))
                {
                    sideCursor = (side + 1) % 4;
                    return true;
                }
            }

            return RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Hostile);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref unlockedLevel, "unlockedLevel", 1);

            Scribe_Values.Look(ref raidActive, "raidActive", false);
            Scribe_Values.Look(ref raidTotalCount, "raidTotalCount", 0);

            Scribe_Collections.Look(ref pending, "pending", LookMode.Deep);
            Scribe_Collections.Look(ref alive, "alive", LookMode.Reference);

            Scribe_References.Look(ref raidFaction, "raidFaction");
            Scribe_References.Look(ref raidLord, "raidLord");

            Scribe_Defs.Look(ref extraHediff, "extraHediff");
            Scribe_Values.Look(ref addExtraHediff, "addExtraHediff", false);
            Scribe_Values.Look(ref sideCursor, "sideCursor", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (pending == null) pending = new List<SpawnRequest>();
                if (alive == null) alive = new List<Pawn>();
                alive.RemoveAll(p => p == null);

                int remaining = (pending?.Count ?? 0) + (alive?.Count ?? 0);
                if (raidTotalCount < remaining) raidTotalCount = remaining;
            }
        }
    }

    public class IncidentWorker_CustomMonsterRaid : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            CustomRaidExtension ext = def.GetModExtension<CustomRaidExtension>();
            if (ext == null) return false;

            Merissu_RaidManager manager = map.GetComponent<Merissu_RaidManager>();
            if (manager == null) return false;

            int currentLevel = manager.unlockedLevel;

            List<PoolData> poolConfig = new List<PoolData>
            {
                new PoolData(1000, ext.poolZayu, ext.hediffZayu, 1),
                new PoolData(2000, ext.poolLow, ext.hediffLow, 2),
                new PoolData(4000, ext.poolMid, ext.hediffMid, 3),
                new PoolData(8000, ext.poolHigh, ext.hediffHigh, 4),
                new PoolData(20000, ext.poolBoss, ext.hediffBoss, 5)
            };

            List<PoolData> activePools = poolConfig
                .Where(p => p.level <= currentLevel && !p.kinds.NullOrEmpty())
                .OrderByDescending(p => p.level)
                .ToList();

            if (activePools.Count == 0) return false;

            float remainingWealth = map.wealthWatcher.WealthTotal;
            Dictionary<int, int> spawnCounts = new Dictionary<int, int>();
            foreach (PoolData p in activePools) spawnCounts[p.cost] = 0;

            foreach (PoolData pool in activePools)
            {
                if (remainingWealth >= pool.cost)
                {
                    spawnCounts[pool.cost]++;
                    remainingWealth -= pool.cost;
                }
            }

            bool canAffordMore = true;
            while (canAffordMore)
            {
                float packageCost = 0f;
                for (int i = 0; i < activePools.Count; i++)
                {
                    int ratio = (int)Math.Pow(2, i);
                    packageCost += activePools[i].cost * ratio;
                }

                if (remainingWealth >= packageCost)
                {
                    for (int i = 0; i < activePools.Count; i++)
                    {
                        int ratio = (int)Math.Pow(2, i);
                        spawnCounts[activePools[i].cost] += ratio;
                    }
                    remainingWealth -= packageCost;
                }
                else
                {
                    canAffordMore = false;
                }
            }

            foreach (PoolData pool in activePools)
            {
                int canAfford = (int)(remainingWealth / pool.cost);
                if (canAfford > 0)
                {
                    spawnCounts[pool.cost] += canAfford;
                    remainingWealth -= canAfford * pool.cost;
                }
            }

            int totalCount = spawnCounts.Values.Sum();
            if (totalCount > 500)
            {
                List<int> costsAsc = spawnCounts.Keys.OrderBy(c => c).ToList();
                foreach (int cost in costsAsc)
                {
                    if (totalCount <= 500) break;
                    int remove = Math.Min(spawnCounts[cost], totalCount - 500);
                    spawnCounts[cost] -= remove;
                    totalCount -= remove;
                }
            }

            bool hasYachieBuff = map.mapPawns.FreeColonists.Any(p =>
                p.health.hediffSet.HasHediff(HediffDef.Named("KitchoYachiecard")));

            List<SpawnRequest> requests = new List<SpawnRequest>();
            foreach (PoolData pool in activePools)
            {
                int count = spawnCounts[pool.cost];
                for (int i = 0; i < count; i++)
                {
                    PawnKindDef kind = pool.kinds.RandomElement();
                    requests.Add(new SpawnRequest(kind, pool.specificHediff));
                }
            }

            if (requests.Count == 0) return false;

            Faction raidFaction = parms.faction ?? Faction.OfEntities;
            manager.StartRaid(requests, raidFaction, ext.hediffExtraF, hasYachieBuff, ext, currentLevel);
            LookTargets lookTargets = new LookTargets(map.Center, map);
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, lookTargets);
            return true;
        }

        private class PoolData
        {
            public int cost;
            public List<PawnKindDef> kinds;
            public HediffDef specificHediff;
            public int level;

            public PoolData(int c, List<PawnKindDef> k, HediffDef h, int l)
            {
                cost = c;
                kinds = k;
                specificHediff = h;
                level = l;
            }
        }
    }
}