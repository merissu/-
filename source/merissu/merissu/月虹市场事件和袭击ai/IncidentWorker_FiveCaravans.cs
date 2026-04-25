using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class IncidentWorker_FiveCaravans : IncidentWorker
    {
        private const string BoostHediffName = "Businessacumen";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int caravanCount = 0;

            bool isKaguyaActive = map.mapPawns.FreeColonists.Any(p => p.health.hediffSet.HasHediff(HediffDef.Named(BoostHediffName)));

            var friendlyFactions = Find.FactionManager.AllFactions
                .Where(f => !f.def.hidden && !f.IsPlayer && !f.HostileTo(Faction.OfPlayer))
                .ToList();

            if (friendlyFactions.Count == 0) return false;

            foreach (Faction faction in friendlyFactions.InRandomOrder().Take(5))
            {
                IncidentParms caravanParms = new IncidentParms { target = map, faction = faction, forced = true };

                if (IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(caravanParms))
                {
                    caravanCount++;

                    Pawn luckyPawn = map.mapPawns.AllPawnsSpawned
                        .Where(p => p.Faction == faction && p.RaceProps.Humanlike)
                        .RandomElementWithFallback();

                    if (luckyPawn != null)
                    {
                        luckyPawn.health.AddHediff(HediffDef.Named("Merissu_SpecialGuestMarker"));

                        string[] targetLayers = { "AuxiliaryEquipmentCard", "AuxiliaryAbilityCard", "ActiveItemCard" };

                        int drawCount = isKaguyaActive ? 3 : 1;

                        foreach (string layer in targetLayers)
                        {
                            var potentialItems = DefDatabase<ThingDef>.AllDefs
                                .Where(d => d.IsApparel && d.apparel.layers.Any(l => l.defName == layer))
                                .ToList();

                            if (potentialItems.Any())
                            {
                                for (int i = 0; i < drawCount; i++)
                                {
                                    ThingDef selected = potentialItems.RandomElement();
                                    Thing card = ThingMaker.MakeThing(selected);
                                    luckyPawn.inventory.innerContainer.TryAdd(card, 1);
                                }
                                ThingDef coinDef = DefDatabase<ThingDef>.GetNamed("Merissu_CustomCoin", false);
                                if (coinDef != null)
                                {
                                    Thing coins = ThingMaker.MakeThing(coinDef);
                                    coins.stackCount = 600;
                                    luckyPawn.inventory.innerContainer.TryAdd(coins, 200);
                                }
                            }
                        }
                    }
                }
            }

            if (caravanCount > 0)
            {
                string msg = isKaguyaActive
                    ? "由于招财猫的能力，到来的商人们携带了远超往常数量的卡牌"
                    : "月虹市场开设了，一群带着不可思议的卡牌的商人到来了。";

                Messages.Message(msg, isKaguyaActive ? MessageTypeDefOf.CautionInput : MessageTypeDefOf.PositiveEvent);
                return true;
            }
            return false;
        }
    }
}