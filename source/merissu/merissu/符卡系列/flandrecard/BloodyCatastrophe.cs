using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class BloodyCatastrophe : Ability
    {
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

            if (thing is Pawn targetPawn)
            {
                DropPawnInventory(targetPawn, pos, map);
                GenerateButcherProducts(targetPawn, pos, map);

                targetPawn.Kill(null);

                if (targetPawn.Corpse != null && !targetPawn.Corpse.Destroyed)
                {
                    targetPawn.Corpse.Destroy(DestroyMode.Vanish);
                }

                return true;
            }

            GenerateBuildingProducts(thing, pos, map);
            thing.Destroy(DestroyMode.KillFinalize);

            return true;
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