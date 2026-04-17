using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic;

namespace merissu
{
    public class Ability_BribingYama : Ability
    {
        public Ability_BribingYama() : base() { }

        public Ability_BribingYama(Pawn pawn) : base(pawn) { }

        public Ability_BribingYama(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public static readonly ThingDef CoinDef = ThingDef.Named("Merissu_CustomCoin");
        public const int RequiredAmount = 50;
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport) return baseReport;

                int count = TotalCoinCount(pawn);
                if (count < RequiredAmount)
                {
                    return "持有金额不足 (当前: " + count + "/" + RequiredAmount + ")";
                }

                return true;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.Activate(target, dest))
            {
                return false;
            }

            ConsumeCoins(pawn, RequiredAmount);
            return true;
        }

        private int TotalCoinCount(Pawn p)
        {
            if (p == null || p.Map == null) return 0;

            int inventoryCount = p.inventory.innerContainer
                .Where(t => t.def == CoinDef)
                .Sum(t => t.stackCount);

            int mapCount = p.Map.resourceCounter.GetCount(CoinDef);
            return inventoryCount + mapCount;
        }

        private void ConsumeCoins(Pawn p, int amount)
        {
            int remaining = amount;

            List<Thing> invItems = p.inventory.innerContainer
                .Where(t => t.def == CoinDef).ToList();
            foreach (var item in invItems)
            {
                int toRemove = System.Math.Min(remaining, item.stackCount);
                p.inventory.innerContainer.Take(item, toRemove).Destroy();
                remaining -= toRemove;
                if (remaining <= 0) return;
            }

            if (remaining > 0)
            {
                List<Thing> mapItems = p.Map.listerThings.ThingsOfDef(CoinDef)
                    .Where(t => !t.IsForbidden(p)).ToList();
                foreach (var item in mapItems)
                {
                    int toRemove = System.Math.Min(remaining, item.stackCount);
                    item.SplitOff(toRemove).Destroy();
                    remaining -= toRemove;
                    if (remaining <= 0) return;
                }
            }
        }
    }
}