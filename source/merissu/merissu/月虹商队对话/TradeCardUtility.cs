using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public static class TradeCardUtility
    {
        public static float GetCustomCardPrice(Thing thing)
        {
            if (thing == null || thing.def == null) return 0f;

            if (thing.def.costList != null)
            {
                foreach (var countClass in thing.def.costList)
                {
                    if (countClass.thingDef.defName == "Merissu_CustomCoin")
                    {
                        return countClass.count / 2f;
                    }
                }
            }
            return 0f;
        }

        public static bool CheckCustomCoinCount(Map map, float amount)
        {
            int totalNeeded = Mathf.CeilToInt(amount);
            ThingDef coinDef = ThingDef.Named("Merissu_CustomCoin");
            int availableCount = 0;

            List<Thing> coins = map.listerThings.ThingsOfDef(coinDef);
            foreach (var c in coins)
            {
                if (!c.IsForbidden(Faction.OfPlayer)) availableCount += c.stackCount;
            }
            return availableCount >= totalNeeded;
        }

        public static bool TryPayWithCustomCoin(Map map, float amount)
        {
            int totalNeeded = Mathf.CeilToInt(amount);
            ThingDef coinDef = ThingDef.Named("Merissu_CustomCoin");

            int availableCount = 0;
            List<Thing> coins = map.listerThings.ThingsOfDef(coinDef);
            foreach (var c in coins)
            {
                if (!c.IsForbidden(Faction.OfPlayer)) availableCount += c.stackCount;
            }

            if (availableCount < totalNeeded) return false;

            int remainingToPay = totalNeeded;
            for (int i = coins.Count - 1; i >= 0 && remainingToPay > 0; i--)
            {
                Thing c = coins[i];
                if (c.IsForbidden(Faction.OfPlayer)) continue;

                int toTake = Mathf.Min(c.stackCount, remainingToPay);
                c.SplitOff(toTake).Destroy();
                remainingToPay -= toTake;
            }
            return true;
        }

        public static void ExecutePhysicalExchange(Pawn buyer, Pawn seller, Thing playerOfferedCard, Thing traderTargetCard)
        {
            bool leadRemoved = false;
            if (playerOfferedCard.holdingOwner != null)
            {
                int transferredCount = playerOfferedCard.holdingOwner.TryTransferToContainer(
                    playerOfferedCard,
                    seller.inventory.innerContainer,
                    playerOfferedCard.stackCount
                );
                leadRemoved = (transferredCount > 0);
            }
            else if (playerOfferedCard.Spawned)
            {
                playerOfferedCard.DeSpawn();
                seller.inventory.innerContainer.TryAdd(playerOfferedCard);
                leadRemoved = true;
            }

            if (traderTargetCard != null)
            {
                if (traderTargetCard.holdingOwner != null)
                {
                    traderTargetCard.holdingOwner.TryDrop(traderTargetCard, buyer.Position, buyer.Map, ThingPlaceMode.Near, out Thing _);
                }
                else if (traderTargetCard.Spawned)
                {
                    IntVec3 targetPos = buyer.Position;
                    Map targetMap = buyer.Map;

                    traderTargetCard.DeSpawn();
                    GenPlace.TryPlaceThing(traderTargetCard, targetPos, targetMap, ThingPlaceMode.Near);
                }
            }
            Messages.Message("卡牌交换完成", MessageTypeDefOf.PositiveEvent);
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
        }

        public static void ExecutePhysicalExchangeOnlyGet(Pawn buyer, Thing traderTargetCard)
        {
            if (traderTargetCard != null)
            {
                if (traderTargetCard.holdingOwner != null)
                {
                    traderTargetCard.holdingOwner.TryDrop(traderTargetCard, buyer.Position, buyer.Map, ThingPlaceMode.Near, out Thing _);
                }
                else if (traderTargetCard.Spawned)
                {
                    IntVec3 targetPos = buyer.Position;
                    Map targetMap = buyer.Map;
                    traderTargetCard.DeSpawn();
                    GenPlace.TryPlaceThing(traderTargetCard, targetPos, targetMap, ThingPlaceMode.Near);
                }
            }
            Messages.Message("购买完成", MessageTypeDefOf.PositiveEvent);
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
        }
    }
}