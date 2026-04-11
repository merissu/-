using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace U.N.Owen
{
    public class CompProperties_AbilityFutatsuiwaClansCurse : CompProperties_AbilityEffectWithDuration
    {
        public HediffDef polymorphStateHediff;
        public bool keepTargetFaction = true;

        public CompProperties_AbilityFutatsuiwaClansCurse()
        {
            compClass = typeof(CompAbilityEffect_FutatsuiwaClansCurse);
        }
    }
    public class CompAbilityEffect_FutatsuiwaClansCurse : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityFutatsuiwaClansCurse Props =>
            (CompProperties_AbilityFutatsuiwaClansCurse)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages)) return false;
            if (target.Pawn == null) return false;
            if (!target.Pawn.Spawned) return false;
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn original = target.Pawn;
            if (original == null || !original.Spawned) return;

            var animalKinds = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(k => k?.RaceProps?.Animal == true)
                .ToList();

            if (animalKinds.Count == 0) return;

            PawnKindDef randomKind = animalKinds.RandomElement();
            Faction faction = Props.keepTargetFaction ? original.Faction : null;

            Pawn animal = PawnGenerator.GeneratePawn(randomKind, faction);

            Map map = original.Map;
            IntVec3 cell = original.Position;
            Rot4 rot = original.Rotation;

            // 暂存原体
            original.DeSpawn();
            if (!Find.WorldPawns.Contains(original))
            {
                Find.WorldPawns.PassToWorld(original, PawnDiscardDecideMode.KeepForever);
            }

            // 生成动物形态
            GenSpawn.Spawn(animal, cell, map, rot);

            // 给动物挂“变身状态”Hediff
            Hediff h = HediffMaker.MakeHediff(Props.polymorphStateHediff, animal);
            var stateComp = h.TryGetComp<HediffComp_FutatsuiwaClansCurse>();
            if (stateComp != null)
            {
                stateComp.originalPawn = original;
            }

            var disappear = h.TryGetComp<HediffComp_Disappears>();
            if (disappear != null)
            {
                disappear.SetDuration(GetDurationSeconds(original).SecondsToTicks());
            }
            this.parent.AddEffecterToMaintain(EffecterDefOf.Mamizou.Spawn(target.Thing, this.parent.pawn.Map, 1f), target.Thing.Position, 60, null);
            animal.health.AddHediff(h);
        }
    }
    public class HediffCompProperties_FutatsuiwaClansCurse : HediffCompProperties
    {
        public DamageDef returnDamageDef = RimWorld.DamageDefOf.Blunt;
        public bool ignoreArmorOnReturnDamage = true;

        public HediffCompProperties_FutatsuiwaClansCurse()
        {
            compClass = typeof(HediffComp_FutatsuiwaClansCurse);
        }
    }
    public class HediffComp_FutatsuiwaClansCurse : HediffComp
    {
        public Pawn originalPawn;
        public float accumulatedDamage;
        private bool reverted;

        public HediffCompProperties_FutatsuiwaClansCurse Props =>
            (HediffCompProperties_FutatsuiwaClansCurse)props;

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (totalDamageDealt > 0f && dinfo.Def.harmsHealth)
            {
                accumulatedDamage += totalDamageDealt;
            }
        }

        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if(!this.Pawn.Spawned)
            {
                Hediff hediff = this.parent;
                this.Pawn.health.AddHediff(hediff, null, null, null);
                hediff.TryGetComp<HediffComp_FutatsuiwaClansCurse>().originalPawn = this.originalPawn;
            }
            EffecterDefOf.Mamizou.SpawnMaintained(this.Pawn.Position, this.Pawn.Map);
            RevertNow();
        }

        private void RevertNow()
        {
            if (reverted) return;
            reverted = true;

            Pawn animal = Pawn;
            if (animal == null) return;

            Map map = animal.MapHeld ?? animal.Corpse?.Map;
            IntVec3 pos = animal.Spawned ? animal.Position : (animal.Corpse?.Position ?? IntVec3.Invalid);
            Rot4 rot = animal.Rotation;

            if (animal.Spawned)
            {
                animal.DeSpawn();
            }
            // 还原原体
            if (originalPawn != null && !originalPawn.Destroyed)
            {
                if (Find.WorldPawns.Contains(originalPawn))
                {
                    Find.WorldPawns.RemovePawn(originalPawn);
                }

                if (map != null && pos.IsValid)
                {
                    GenSpawn.Spawn(originalPawn, pos, map, rot);
                    originalPawn.SetFaction(this.Pawn.Faction);
                    if (accumulatedDamage > 0f && !originalPawn.Destroyed)
                    {
                        DamageInfo dinfo = new DamageInfo(
                            Props.returnDamageDef ?? RimWorld.DamageDefOf.Blunt,
                            accumulatedDamage,
                            999f
                        );
                        if (Props.ignoreArmorOnReturnDamage)
                            dinfo.SetIgnoreArmor(true);

                        originalPawn.TakeDamage(dinfo);
                    }
                }
                else
                {
                    // 无地图时只能先放回世界池
                    Find.WorldPawns.PassToWorld(originalPawn, PawnDiscardDecideMode.KeepForever);
                }
            }

            // 丢弃临时动物 Pawn
            if (!animal.Destroyed)
            {
                Find.WorldPawns.PassToWorld(animal, PawnDiscardDecideMode.Discard);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref originalPawn, "originalPawn");
            Scribe_Values.Look(ref accumulatedDamage, "accumulatedDamage", 0f);
            Scribe_Values.Look(ref reverted, "reverted", false);
        }
    }
    public class CompProperties_AbilityYoukaiWorldGateofOneHundredDemons : CompProperties_AbilityEffectWithDuration
    {
        public ThingDef riftThingDef;

        public CompProperties_AbilityYoukaiWorldGateofOneHundredDemons()
        {
            compClass = typeof(CompAbilityEffect_YoukaiWorldGateofOneHundredDemons);
        }
    }
    public class CompAbilityEffect_YoukaiWorldGateofOneHundredDemons : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilityYoukaiWorldGateofOneHundredDemons Props =>
            (CompProperties_AbilityYoukaiWorldGateofOneHundredDemons)props;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return parent.pawn?.Spawned == true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || Props.riftThingDef == null) return;

            IntVec3 backCell = caster.Position + caster.Rotation.Opposite.FacingCell;
            if (!backCell.InBounds(caster.Map) || !backCell.Standable(caster.Map))
                backCell = CellFinder.RandomClosewalkCellNear(caster.Position, caster.Map, 2);

            Thing rift = ThingMaker.MakeThing(Props.riftThingDef);
            GenSpawn.Spawn(rift, backCell, caster.Map);
            this.parent.AddEffecterToMaintain(EffecterDefOf.Mamizou.Spawn(target.Cell, this.parent.pawn.Map, 1f), target.Cell, 60, null);
            CompYoukaiWorldGateofOneHundredDemons comp = rift.TryGetComp<CompYoukaiWorldGateofOneHundredDemons>();
            if (comp != null)
            {
                int durationTicks = GetDurationSeconds(caster).SecondsToTicks();
                comp.Initialize(caster, durationTicks);
            }
        }
    }
    public class CompProperties_YoukaiWorldGateofOneHundredDemons : CompProperties
    {
        public int spawnIntervalTicks = 20;   // 3秒一只
        public int commandIntervalTicks = 90;  // 索敌刷新
        public int fleckIntervalTicks = 6;
        public int summonLifetimeTicks = 2500; // 每只存在时间
        public float searchEnemyRange = 40f;
        public int spawnRadius = 2;

        public CompProperties_YoukaiWorldGateofOneHundredDemons()
        {
            compClass = typeof(CompYoukaiWorldGateofOneHundredDemons);
        }
    }
    public class CompYoukaiWorldGateofOneHundredDemons : ThingComp
    {
        private Pawn caster;
        private Faction faction;
        private int endTick;
        private int nextSpawnTick;
        private int nextCmdTick;
        private int nextFleckTick;

        private List<Pawn> summons = new List<Pawn>();
        private List<int> expireTicks = new List<int>();

        public CompProperties_YoukaiWorldGateofOneHundredDemons Props => (CompProperties_YoukaiWorldGateofOneHundredDemons)props;

        public void Initialize(Pawn casterPawn, int durationTicks)
        {
            caster = casterPawn;
            faction = casterPawn?.Faction;
            int now = Find.TickManager.TicksGame;
            endTick = now + durationTicks;
            nextSpawnTick = now + Props.spawnIntervalTicks;
            nextCmdTick = now + Props.commandIntervalTicks;
            nextFleckTick = now + Props.fleckIntervalTicks;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.Map == null) return;

            int now = Find.TickManager.TicksGame;

            CleanupExpired(now);

            if (now >= nextSpawnTick && now < endTick)
            {
                nextCmdTick = now + Props.commandIntervalTicks;
                CommandSummonsAttack();
                nextSpawnTick = now + Props.spawnIntervalTicks;
                SpawnOne();
                EffecterDefOf.Mamizou.SpawnMaintained(this.parent.Position, this.parent.Map);

            }

            if (now >= endTick)
            {
                parent.Destroy(DestroyMode.Vanish);
            }
        }

        private void SpawnOne()
        {
            if (parent.Map == null) return;

            var pool = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(k => k?.RaceProps?.Animal == true)
                .ToList();
            if (pool.Count == 0) return;

            PawnKindDef kind = pool.RandomElement();

            IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, Props.spawnRadius);
            Pawn p = PawnGenerator.GeneratePawn(kind, faction);
            GenSpawn.Spawn(p, spawnCell, parent.Map);

            summons.Add(p);
            expireTicks.Add(Find.TickManager.TicksGame + Props.summonLifetimeTicks);

            OrderAttack(p);
        }

        private void CommandSummonsAttack()
        {
            for (int i = summons.Count - 1; i >= 0; i--)
            {
                Pawn p = summons[i];
                if (p == null || p.Destroyed || !p.Spawned)
                    continue;

                OrderAttack(p);
            }
        }

        private void OrderAttack(Pawn p)
        {
            Pawn target = GenClosest.ClosestThingReachable(
                p.Position, p.Map,
                ThingRequest.ForGroup(ThingRequestGroup.AttackTarget),
                PathEndMode.Touch,
                TraverseParms.For(p),
                Props.searchEnemyRange,
                t => t is Pawn tp && tp.Spawned && tp.HostileTo(p)
            ) as Pawn;

            if (target == null) return;

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private void CleanupExpired(int now)
        {
            for (int i = summons.Count - 1; i >= 0; i--)
            {
                Pawn p = summons[i];
                bool remove = p == null || p.Destroyed;

                if (!remove && now >= expireTicks[i])
                {
                    if (p.Spawned) p.Destroy(DestroyMode.Vanish);
                    else p.Discard();
                    remove = true;
                }

                if (remove)
                {
                    summons.RemoveAt(i);
                    expireTicks.RemoveAt(i);
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref endTick, "endTick");
            Scribe_Values.Look(ref nextSpawnTick, "nextSpawnTick");
            Scribe_Values.Look(ref nextCmdTick, "nextCmdTick");
            Scribe_Values.Look(ref nextFleckTick, "nextFleckTick");
            Scribe_Collections.Look(ref summons, "summons", LookMode.Reference);
            Scribe_Collections.Look(ref expireTicks, "expireTicks", LookMode.Value);
        }
    }
}
