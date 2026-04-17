using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

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

        public List<PawnKindDef> poolZayu;
        public List<PawnKindDef> poolLow;
        public List<PawnKindDef> poolMid;
        public List<PawnKindDef> poolHigh;
        public List<PawnKindDef> poolBoss;
    }

    public class Merissu_RaidManager : MapComponent
    {
        public int unlockedLevel = 1;
        public Merissu_RaidManager(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref unlockedLevel, "unlockedLevel", 1);
        }
    }

    public class IncidentWorker_CustomMonsterRaid : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var ext = def.GetModExtension<CustomRaidExtension>();
            if (ext == null) return false;

            var manager = map.GetComponent<Merissu_RaidManager>();
            int currentLevel = manager?.unlockedLevel ?? 1;

            var poolConfig = new List<PoolData>
            {
                new PoolData(1000, ext.poolZayu, ext.hediffZayu, 1),
                new PoolData(2000, ext.poolLow, ext.hediffLow, 2),
                new PoolData(4000, ext.poolMid, ext.hediffMid, 3),
                new PoolData(8000, ext.poolHigh, ext.hediffHigh, 4),
                new PoolData(20000, ext.poolBoss, ext.hediffBoss, 5)
            };

            var activePools = poolConfig
                .Where(p => p.level <= currentLevel && !p.kinds.NullOrEmpty())
                .OrderByDescending(p => p.level)
                .ToList();

            if (activePools.Count == 0) return false;

            float remainingWealth = map.wealthWatcher.WealthTotal;
            Dictionary<int, int> spawnCounts = new Dictionary<int, int>();
            foreach (var p in activePools) spawnCounts[p.cost] = 0;

            foreach (var pool in activePools)
            {
                if (remainingWealth >= pool.cost)
                {
                    spawnCounts[pool.cost]++;
                    remainingWealth -= pool.cost;
                }
            }

            if (activePools.Count > 0)
            {
                bool canAffordMore = true;
                while (canAffordMore)
                {
                    float packageCost = 0;
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
            }

            foreach (var pool in activePools)
            {
                int canAfford = (int)(remainingWealth / pool.cost);
                if (canAfford > 0)
                {
                    spawnCounts[pool.cost] += canAfford;
                    remainingWealth -= canAfford * pool.cost;
                }
            }

            int totalCount = spawnCounts.Values.Sum();
            if (totalCount > 200)
            {
                var costsAsc = spawnCounts.Keys.OrderBy(c => c).ToList();
                for (int i = 0; i < costsAsc.Count - 1; i++)
                {
                    int currentCost = costsAsc[i];
                    int nextCost = costsAsc[i + 1];
                    while (spawnCounts[currentCost] > 0 && totalCount > 200)
                    {
                        spawnCounts[currentCost]--;
                        totalCount--;
                        remainingWealth += currentCost;
                        if (remainingWealth >= nextCost)
                        {
                            spawnCounts[nextCost]++;
                            totalCount++;
                            remainingWealth -= nextCost;
                        }
                    }
                }
                while (spawnCounts.Values.Sum() > 200)
                {
                    foreach (var cost in costsAsc)
                    {
                        if (spawnCounts[cost] > 0) { spawnCounts[cost]--; break; }
                    }
                }
            }

            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 cell, map, CellFinder.EdgeRoadChance_Hostile)) return false;

            bool hasYachieBuff = map.mapPawns.FreeColonists.Any(p =>
                p.health.hediffSet.HasHediff(HediffDef.Named("KitchoYachiecard")));

            List<Pawn> spawnedPawns = new List<Pawn>();
            foreach (var pool in activePools)
            {
                int count = spawnCounts[pool.cost];
                for (int i = 0; i < count; i++)
                {
                    PawnKindDef kind = pool.kinds.RandomElement();
                    Pawn pawn = PawnGenerator.GeneratePawn(kind, null);
                    GenSpawn.Spawn(pawn, cell, map);

                    if (pool.specificHediff != null)
                        pawn.health.AddHediff(pool.specificHediff);

                    if (hasYachieBuff && ext.hediffExtraF != null)
                        pawn.health.AddHediff(ext.hediffExtraF);

                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    spawnedPawns.Add(pawn);
                }
            }

            if (spawnedPawns.Count > 0)
            {
                SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, spawnedPawns);
                return true;
            }
            return false;
        }

        private class PoolData
        {
            public int cost;
            public List<PawnKindDef> kinds;
            public HediffDef specificHediff;
            public int level;
            public PoolData(int c, List<PawnKindDef> k, HediffDef h, int l)
            {
                cost = c; kinds = k; specificHediff = h; level = l;
            }
        }
    }
}