using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

    namespace U.N.Owen.SupernaturalBead
    {
        [DefOf]
        public static class ThingDefOf
        {
            static ThingDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
            }
            public static ThingDef SupernaturalBead;
        }
        public class GenStep_OutpostRobust : GenStep
        {
            public int size = 220;
            public int requiredWorshippedTerminalRooms;
            public int requiredGravcoreRooms;
            public bool allowGeneratingThronerooms = true;
            public bool settlementDontGeneratePawns;
            public bool allowGeneratingFarms = false;
            public bool generateLoot = true;
            public MapGenUtility.PostProcessSettlementParams postProcessSettlementParams;
            public bool unfogged;
            public bool attackWhenPlayerBecameEnemy;
            public FloatRange defaultPawnGroupPointsRange = SymbolResolver_Settlement.DefaultPawnsPoints;
            public PawnGroupKindDef pawnGroupKindDef;
            public CellRect? forcedRect;
            public Faction overrideFaction;

            private static readonly List<CellRect> possibleRects = new List<CellRect>();
            private bool WillPostProcess => postProcessSettlementParams != null;
            public override int SeedPart => 398638182; // 改个seed避免和原版完全一致

            public override void Generate(Map map, GenStepParams parms)
            {
                List<CellRect> usedRects = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");

                CellRect anchor;
                bool hasROI = MapGenerator.TryGetVar<CellRect>("RectOfInterest", out anchor);

                if (!hasROI)
                {
                    if (!TryFindAnchorRect(map, usedRects, out anchor))
                    {
                        // 关键 fallback：不再报错退出
                        int w = Mathf.Min(size, map.Size.x - 4);
                        int h = Mathf.Min(size, map.Size.z - 4);
                        anchor = CellRect.CenteredOn(map.Center, w, h);
                        anchor.ClipInsideMap(map);
                    }
                }

                Faction faction = overrideFaction
                    ?? ((map.ParentFaction != null && map.ParentFaction != Faction.OfPlayer)
                        ? map.ParentFaction
                        : Find.FactionManager.RandomEnemyFaction());

                ResolveParams rp = new ResolveParams
                {
                    rect = forcedRect ?? GetOutpostRect(anchor, usedRects, map),
                    faction = faction,
                    edgeDefenseWidth = 2,
                    edgeDefenseTurretsCount = Rand.RangeInclusive(0, 1),
                    edgeDefenseMortarsCount = 0,
                    settlementDontGeneratePawns = settlementDontGeneratePawns,
                    attackWhenPlayerBecameEnemy = attackWhenPlayerBecameEnemy,
                    pawnGroupKindDef = pawnGroupKindDef
                };

                if (parms.sitePart != null)
                {
                    rp.bedCount = parms.sitePart.expectedEnemyCount == -1 ? 0 : parms.sitePart.expectedEnemyCount;
                    rp.sitePart = parms.sitePart;
                    rp.settlementPawnGroupPoints = parms.sitePart.parms.threatPoints;
                    rp.settlementPawnGroupSeed = OutpostSitePartUtility.GetPawnGroupMakerSeed(parms.sitePart.parms);
                }
                else
                {
                    rp.settlementPawnGroupPoints = defaultPawnGroupPointsRange.RandomInRange;
                }

                rp.allowGeneratingThronerooms = allowGeneratingThronerooms;
                rp.lootMarketValue = generateLoot
                    ? (parms.sitePart != null ? parms.sitePart.parms.lootMarketValue : 0)
                    : 0f;

                BaseGen.globalSettings.map = map;
                BaseGen.globalSettings.minBuildings = requiredWorshippedTerminalRooms + requiredGravcoreRooms + 1;
                BaseGen.globalSettings.minBarracks = 1;
                BaseGen.globalSettings.requiredWorshippedTerminalRooms = requiredWorshippedTerminalRooms;
                BaseGen.globalSettings.requiredGravcoreRooms = requiredGravcoreRooms;
                BaseGen.globalSettings.maxFarms = allowGeneratingFarms ? -1 : 0;

                BaseGen.symbolStack.Push("settlement", rp);

                if (faction == Faction.OfEmpire)
                {
                    BaseGen.globalSettings.minThroneRooms = allowGeneratingThronerooms ? 1 : 0;
                    BaseGen.globalSettings.minLandingPads = 1;
                }

                List<Building> previous = null;
                if (WillPostProcess)
                    previous = new List<Building>(map.listerThings.GetThingsOfType<Building>());

                BaseGen.Generate();

                if (faction == Faction.OfEmpire && BaseGen.globalSettings.landingPadsGenerated == 0)
                {
                    GenStep_Settlement.GenerateLandingPadNearby(rp.rect, map, faction, out var usedRect);
                    usedRects.Add(usedRect);
                }

                if (WillPostProcess)
                {
                    List<Building> placed = map.listerThings.GetThingsOfType<Building>()
                        .Where(b => !previous.Contains(b)).ToList();
                    MapGenUtility.PostProcessSettlement(map, placed, postProcessSettlementParams);
                }

                if (unfogged)
                    foreach (IntVec3 c in rp.rect) MapGenerator.rootsToUnfog.Add(c);

                usedRects.Add(rp.rect);
            }

            private bool TryFindAnchorRect(Map map, List<CellRect> usedRects, out CellRect rect)
            {
                bool Validator(CellRect r)
                {
                    if (usedRects.Any(u => u.Overlaps(r))) return false;
                    // 放宽限制：不再要求离中心<0.75*地图宽，也不限制水占比10%
                    return true;
                }

                // 先试标准 clear rect（但放宽 elevation/border）
                if (MapGenUtility.TryGetClosestClearRectTo(out rect, new IntVec2(size, size), map.Center, Validator,
                        minElevation: -1f, maxElevation: 1f, mapBorderPadding: 1))
                    return true;

                if (MapGenUtility.TryGetRandomClearRect(size, size, out rect, -1, -1, Validator,
                        minElevation: -1f, maxElevation: 1f, mapBorderPadding: 1))
                    return true;

                rect = CellRect.Empty;
                return false;
            }

            private CellRect GetOutpostRect(CellRect rectToDefend, List<CellRect> usedRects, Map map)
            {
                possibleRects.Clear();
                possibleRects.Add(new CellRect(rectToDefend.minX - 1 - size, rectToDefend.CenterCell.z - size / 2, size, size));
                possibleRects.Add(new CellRect(rectToDefend.maxX + 1, rectToDefend.CenterCell.z - size / 2, size, size));
                possibleRects.Add(new CellRect(rectToDefend.CenterCell.x - size / 2, rectToDefend.minZ - 1 - size, size, size));
                possibleRects.Add(new CellRect(rectToDefend.CenterCell.x - size / 2, rectToDefend.maxZ + 1, size, size));

                CellRect mapRect = new CellRect(0, 0, map.Size.x, map.Size.z);
                possibleRects.RemoveAll(x => !x.FullyContainedWithin(mapRect));

                if (!possibleRects.Any()) return rectToDefend;

                var source = possibleRects.Where(x => !usedRects.Any(y => x.Overlaps(y)));
                if (source.Any()) return source.RandomElement();
                return possibleRects.RandomElement();
            }
        }
        public class StorytellerCompProperties_EnemyShrineSite : StorytellerCompProperties
        {
            public IncidentDef incident;
            public float baseMtbDays = 14f;
            public float minDaysPassed = 20f;

            public StorytellerCompProperties_EnemyShrineSite()
            {
                compClass = typeof(StorytellerComp_EnemyShrineSite);
            }
        }
        public class StorytellerComp_EnemyShrineSite : StorytellerComp
        {
            private StorytellerCompProperties_EnemyShrineSite Props
                => (StorytellerCompProperties_EnemyShrineSite)props;

            public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
            {
                if ((float)Find.TickManager.TicksGame < Props.minDaysPassed * 60000f)
                    yield break;

                if (Props.incident == null || !Props.incident.TargetAllowed(target))
                    yield break;

                if (Rand.MTBEventOccurs(Props.baseMtbDays, 60000f, 1000f))
                {
                    IncidentParms parms = GenerateParms(Props.incident.category, target);
                    if (Props.incident.Worker.CanFireNow(parms))
                        yield return new FiringIncident(Props.incident, this, parms);
                }
            }
        }
        public class GenStep_CentralShrineTemple : GenStep
        {
            public ThingDef rewardThingDef = ThingDefOf.SupernaturalBead;              // 你的模组物品
            public ThingDef reliquaryDef;               // 可选，不填则尝试 defName=Reliquary
            public ThingDef wallStuff = RimWorld.ThingDefOf.Granite;
            public IntVec2 templeSize = new IntVec2(17, 17);

            public override int SeedPart => 89123741;

            public override void Generate(Map map, GenStepParams parms)
            {
                CellRect rect = CellRect.CenteredOn(map.Center, templeSize.x, templeSize.z);
                rect.ClipInsideMap(map);

                // 清理中心区域（不动Pawn）
                foreach (IntVec3 c in rect)
                {
                    List<Thing> things = map.thingGrid.ThingsListAt(c).ToList();
                    for (int i = things.Count - 1; i >= 0; i--)
                    {
                        Thing t = things[i];
                        if (t is Pawn) continue;
                        if (t.def.destroyable)
                            t.Destroy(DestroyMode.Vanish);
                    }
                }

                // 外墙
                foreach (IntVec3 c in rect.EdgeCells)
                {
                    if (!c.InBounds(map) || c.Impassable(map)) continue;
                    Thing wall = ThingMaker.MakeThing(RimWorld.ThingDefOf.Wall, wallStuff);
                    GenSpawn.Spawn(wall, c, map, WipeMode.Vanish);
                }

                // 门（南侧）
                IntVec3 doorCell = new IntVec3(rect.CenterCell.x, 0, rect.minZ);
                if (doorCell.InBounds(map))
                {
                    List<Thing> old = map.thingGrid.ThingsListAt(doorCell).ToList();
                    foreach (Thing t in old)
                        if (!(t is Pawn) && t.def.destroyable) t.Destroy(DestroyMode.Vanish);

                    Thing door = ThingMaker.MakeThing(RimWorld.ThingDefOf.Door, wallStuff);
                    GenSpawn.Spawn(door, doorCell, map, WipeMode.Vanish);
                }

                // 中央建筑（可选）
                ThingDef shrineThing = reliquaryDef ?? DefDatabase<ThingDef>.GetNamedSilentFail("Reliquary");
                if (shrineThing != null)
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(shrineThing), rect.CenterCell, map, WipeMode.Vanish);
                }

                // 奖励物品
                if (rewardThingDef != null)
                {
                    Thing reward = ThingMaker.MakeThing(rewardThingDef);
                    IntVec3 dropSpot = rect.CenterCell + IntVec3.North;
                    if (!dropSpot.InBounds(map)) dropSpot = rect.CenterCell;
                    GenPlace.TryPlaceThing(reward, dropSpot, map, ThingPlaceMode.Near);
                }
            }
        }
    }

