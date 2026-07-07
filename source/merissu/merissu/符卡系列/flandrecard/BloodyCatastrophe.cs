using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class BloodyCatastrophe : Ability
    {
        private bool chainActive;
        private int chainTimer;
        private Pawn currentChainTarget;

        private MentalStateDef terrifyingHallucinationsDef;

        public BloodyCatastrophe() : base() { }

        public BloodyCatastrophe(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || target.Thing == null || target.ThingDestroyed)
                return false;

            Thing thing = target.Thing;
            Map map = thing.Map;
            IntVec3 pos = thing.Position;
            Pawn caster = pawn;

            if (terrifyingHallucinationsDef == null)
                terrifyingHallucinationsDef = DefDatabase<MentalStateDef>.GetNamed("TerrifyingHallucinations");

            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: 0.9f,
                damType: DamageDefOf.Bomb,
                instigator: caster,
                damAmount: 50,
                armorPenetration: 0.2f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: false,
                ignoredThings: new List<Thing> { caster }
            );

            if (thing is Pawn targetPawn)
            {
                DropPawnInventory(targetPawn, pos, map);
                GenerateButcherProducts(targetPawn, pos, map);

                targetPawn.Kill(null);

                if (targetPawn.Corpse != null && !targetPawn.Corpse.Destroyed)
                {
                    targetPawn.Corpse.Destroy(DestroyMode.Vanish);
                }

                StartChainReaction(pos, map);

                return true;
            }

            GenerateBuildingProducts(thing, pos, map);
            thing.Destroy(DestroyMode.KillFinalize);

            return true;
        }

        public override void AbilityTick()
        {
            base.AbilityTick();

            if (!chainActive || currentChainTarget == null || currentChainTarget.Destroyed)
                return;

            chainTimer--;
            if (chainTimer <= 0)
            {
                Map map = currentChainTarget.Map;
                IntVec3 pos = currentChainTarget.Position;

                if (map != null)
                {
                    GenExplosion.DoExplosion(
                        center: pos,
                        map: map,
                        radius: 0.9f,
                        damType: DamageDefOf.Bomb,
                        instigator: pawn,
                        damAmount: 50,
                        armorPenetration: 0.2f,
                        explosionSound: null,
                        weapon: null,
                        projectile: null,
                        intendedTarget: null,
                        postExplosionSpawnThingDef: null,
                        postExplosionSpawnChance: 0f,
                        postExplosionSpawnThingCount: 1,
                        applyDamageToExplosionCellsNeighbors: false,
                        preExplosionSpawnThingDef: null,
                        preExplosionSpawnChance: 0f,
                        preExplosionSpawnThingCount: 1,
                        chanceToStartFire: 0f,
                        damageFalloff: false,
                        ignoredThings: new List<Thing> { pawn }
                    );

                    DropPawnInventory(currentChainTarget, pos, map);
                    GenerateButcherProducts(currentChainTarget, pos, map);

                    currentChainTarget.Kill(null);

                    if (currentChainTarget.Corpse != null && !currentChainTarget.Corpse.Destroyed)
                    {
                        currentChainTarget.Corpse.Destroy(DestroyMode.Vanish);
                    }

                    StartChainReaction(pos, map);
                }
                else
                {
                    chainActive = false;
                    currentChainTarget = null;
                }
            }
        }

        private void StartChainReaction(IntVec3 pos, Map map)
        {
            List<Pawn> enemies = new List<Pawn>();
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pos, map, 10f, useCenter: true))
            {
                if (thing is Pawn p && !p.Dead && !p.Downed && p.HostileTo(pawn))
                {
                    enemies.Add(p);
                }
            }

            if (enemies.Count == 0)
            {
                chainActive = false;
                currentChainTarget = null;
                return;
            }

            foreach (Pawn enemy in enemies)
            {
                MentalStateDef stateDef = Rand.Value < 0.5f
                    ? DefDatabase<MentalStateDef>.GetNamed("TerrifyingHallucinations")
                    : MentalStateDefOf.PanicFlee;

                enemy.mindState.mentalStateHandler.TryStartMentalState(
                    stateDef,
                    "Struck with terror by the destruction!",
                    forced: true
                );
            }

            currentChainTarget = enemies.RandomElement();
            chainTimer = 60;
            chainActive = true;
        }

        private void DropPawnInventory(Pawn pawn, IntVec3 pos, Map map)
        {
            pawn.equipment?.DropAllEquipment(pos);
            pawn.inventory?.DropAllNearPawn(pos, forbid: false);
            pawn.apparel?.DropAll(pos);
        }

        private void GenerateButcherProducts(Pawn pawn, IntVec3 pos, Map map)
        {
            if (pawn.def.butcherProducts.NullOrEmpty()) return;

            foreach (ThingDefCountClass product in pawn.def.butcherProducts)
            {
                Thing thing = ThingMaker.MakeThing(product.thingDef);
                thing.stackCount = GenMath.RoundRandom(product.count);
                GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
            }
        }

        private void GenerateBuildingProducts(Thing thing, IntVec3 pos, Map map)
        {
            if (thing.def.costList == null) return;

            foreach (ThingDefCountClass cost in thing.def.costList)
            {
                Thing material = ThingMaker.MakeThing(cost.thingDef);
                material.stackCount = cost.count;
                GenPlace.TryPlaceThing(material, pos, map, ThingPlaceMode.Near);
            }
        }
    }
}