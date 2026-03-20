using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class Verb_Catadioptric : Verb_MeleeAttack
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();

            if (target.Thing == null || target.ThingDestroyed)
                return result;

            Map map = target.Thing.Map;
            IntVec3 pos = target.Thing.Position;
            Pawn caster = CasterPawn;

            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: 1.9f,
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

            if (target.Thing is Pawn pawn)
            {
                DropPawnInventory(pawn, pos, map);

                GenerateButcherProducts(pawn, pos, map);

                pawn.Kill(null);

                if (pawn.Corpse != null && !pawn.Corpse.Destroyed)
                {
                    pawn.Corpse.Destroy(DestroyMode.Vanish);
                }

                return result;
            }

            Thing thing = target.Thing;
            GenerateBuildingProducts(thing, pos, map);
            thing.Destroy(DestroyMode.KillFinalize);

            return result;
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