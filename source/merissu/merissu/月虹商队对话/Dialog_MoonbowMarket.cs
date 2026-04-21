using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Dialog_MoonbowMarket : Window
    {
        private Pawn buyer;
        private Pawn seller;
        private int currentIndex = 0;
        private int lastIndex = 0;

        private bool isSellMode = false;

        private float displayX = 0f;
        private const float MoveSpeed = 800f;
        private const float AreaWidth = 400f;
        private Vector2 descriptionScrollVector = Vector2.zero;

        private static readonly Texture2D TitleTex = ContentFinder<Texture2D>.Get("Accessory/card/Numbers/title");
        private static readonly Texture2D MoneyIcon = ContentFinder<Texture2D>.Get("Accessory/card/money");
        private static readonly string NumberPathPrefix = "Accessory/card/Numbers/";

        private static readonly Texture2D TexEquip = ContentFinder<Texture2D>.Get("Accessory/card/Numbers/AuxiliaryEquipmentCard");
        private static readonly Texture2D TexAbility = ContentFinder<Texture2D>.Get("Accessory/card/Numbers/AuxiliaryAbilityCard");
        private static readonly Texture2D TexActive = ContentFinder<Texture2D>.Get("Accessory/card/Numbers/ActiveItemCard");

        private Dictionary<ThingDef, int> dynamicPriceBook = new Dictionary<ThingDef, int>();
        private List<Thing> availableStock = new List<Thing>();
        private static readonly string[] targetLayers = { "AuxiliaryEquipmentCard", "AuxiliaryAbilityCard", "ActiveItemCard" };

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_MoonbowMarket(Pawn buyer, Pawn seller)
        {
            this.buyer = buyer;
            this.seller = seller;
            this.forcePause = true;
            this.doCloseX = true;

            RefreshStock();
        }

        private void RefreshStock()
        {
            availableStock.Clear();
            dynamicPriceBook.Clear();

            Pawn sourcePawn = isSellMode ? buyer : seller;
            ThingDef coinDef = ThingDef.Named("Merissu_CustomCoin");

            bool hasShoppingTechnique = buyer.health.hediffSet.HasHediff(HediffDef.Named("ShoppingTechniques"));

            var potentialItems = sourcePawn.inventory.innerContainer.ToList();
            if (sourcePawn.apparel != null)
            {
                potentialItems.AddRange(sourcePawn.apparel.WornApparel.Cast<Thing>());
            }

            foreach (Thing t in potentialItems)
            {
                if (t.def.IsApparel && t.def.apparel.layers.Any(l => targetLayers.Contains(l.defName)))
                {
                    int rawPrice = 0;
                    if (t.def.costList != null)
                    {
                        var costEntry = t.def.costList.FirstOrDefault(c => c.thingDef == coinDef);
                        if (costEntry != null) rawPrice = costEntry.count;
                    }

                    int baseBuyPrice = Mathf.Max(1, rawPrice / 2);
                    int finalPrice;

                    if (isSellMode)
                    {
                        finalPrice = Mathf.Max(1, Mathf.RoundToInt(baseBuyPrice * 0.5f));
                    }
                    else
                    {
                        float discount = hasShoppingTechnique ? 0.8f : 1f;
                        finalPrice = Mathf.Max(1, Mathf.RoundToInt(baseBuyPrice * discount));
                    }

                    if (!dynamicPriceBook.ContainsKey(t.def))
                    {
                        dynamicPriceBook.Add(t.def, finalPrice);
                    }
                    availableStock.Add(t);
                }
            }

            if (currentIndex >= availableStock.Count) currentIndex = 0;
        }

        public override void DoWindowContents(Rect inRect)
        {
            DrawHeader(inRect);

            if (availableStock.NullOrEmpty())
            {
                DrawEmptyMsg(inRect);
                DrawModeToggleButton(inRect);
                return;
            }

            if (displayX != 0f) displayX = Mathf.MoveTowards(displayX, 0f, MoveSpeed * Time.deltaTime);

            Thing currentItem = availableStock[currentIndex];
            float areaX = (inRect.width - AreaWidth) / 2f;
            Rect photoArea = new Rect(areaX, 110, AreaWidth, 210);

            GUI.BeginGroup(photoArea);
            float imgSize = 200f;
            float centerX = (AreaWidth - imgSize) / 2f;
            Rect currentImgRect = new Rect(centerX + displayX, 0f, imgSize, imgSize);
            Widgets.ThingIcon(currentImgRect, currentItem.def);
            if (displayX != 0f)
            {
                float sideOffset = (displayX > 0) ? -AreaWidth : AreaWidth;
                Widgets.ThingIcon(new Rect(centerX + displayX + sideOffset, 0f, imgSize, imgSize), availableStock[lastIndex].def);
            }
            GUI.EndGroup();

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect titleRect = new Rect(0f, photoArea.yMax - 45f, inRect.width, 35f);
            Widgets.Label(titleRect, $"{currentItem.LabelCap}");

            Text.Font = GameFont.Small;
            string filteredDesc = Regex.Replace(currentItem.def.DescriptionDetailed, @"^(层|覆盖|Layer|Coverage)\s*:.*$", "", RegexOptions.Multiline).Trim();
            Rect descOutRect = new Rect(60f, titleRect.yMax, inRect.width - 120f, 150f);
            float textHeight = Text.CalcHeight(filteredDesc, descOutRect.width - 16f);
            Widgets.BeginScrollView(descOutRect, ref descriptionScrollVector, new Rect(0f, 0f, descOutRect.width - 16f, textHeight));
            Widgets.Label(new Rect(0f, 0f, descOutRect.width - 16f, textHeight), filteredDesc);
            Widgets.EndScrollView();

            DrawLayerBanner(currentItem, descOutRect);
            DrawControls(inRect);
            DrawModeToggleButton(inRect);
        }

        private void DrawModeToggleButton(Rect inRect)
        {
            Rect btnRect = new Rect(inRect.width - 110f, inRect.height - 50f, 100f, 30f);
            string label = isSellMode ? "切换:买入" : "切换:卖出";
            if (Widgets.ButtonText(btnRect, label))
            {
                isSellMode = !isSellMode;
                currentIndex = 0;
                displayX = 0;
                RefreshStock();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawControls(Rect inRect)
        {
            bool canSwitch = (displayX == 0f);
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 260f, 200f, 40f, 40f), " < ") && canSwitch) { SwitchIndex(-1); }
            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 220f, 200f, 40f, 40f), " > ") && canSwitch) { SwitchIndex(1); }

            Thing currentItem = availableStock[currentIndex];
            int price = dynamicPriceBook[currentItem.def];

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, inRect.height - 110f, inRect.width, 30f), $"{(isSellMode ? "回收价" : "售价")}: {price}");

            float btnWidth = 120f;
            float btnHeight = 40f;
            float spacing = 20f;
            float totalWidth = (btnWidth * 2) + spacing;
            float startX = (inRect.width - totalWidth) / 2f;

            if (Widgets.ButtonText(new Rect(startX, inRect.height - 70f, btnWidth, btnHeight), "交换卡牌"))
            {
                Find.WindowStack.Add(new Dialog_TradeTalk(buyer, seller));
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            string actionLabel = isSellMode ? "确认卖出" : "确认购买";
            if (Widgets.ButtonText(new Rect(startX + btnWidth + spacing, inRect.height - 70f, btnWidth, btnHeight), actionLabel))
            {
                if (isSellMode) TrySellItem(currentItem, price);
                else TryPurchaseRealItem(currentItem, price);
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void SwitchIndex(int dir)
        {
            lastIndex = currentIndex;
            currentIndex = (currentIndex + dir + availableStock.Count) % availableStock.Count;
            displayX = dir > 0 ? AreaWidth : -AreaWidth;
            descriptionScrollVector = Vector2.zero;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }

        private void TrySellItem(Thing item, int price)
        {
            ThingDef coinDef = ThingDef.Named("Merissu_CustomCoin");

            int sellerMoney = seller.inventory.innerContainer.TotalStackCountOfDef(coinDef);
            if (sellerMoney < price)
            {
                Messages.Message("商人没有足够的金钱来支付！", MessageTypeDefOf.RejectInput);
                return;
            }

            SpendCoinsFromSeller(coinDef, price);

            Thing playerCoin = ThingMaker.MakeThing(coinDef);
            playerCoin.stackCount = price;
            if (!buyer.inventory.innerContainer.TryAdd(playerCoin))
            {
                GenPlace.TryPlaceThing(playerCoin, buyer.Position, buyer.Map, ThingPlaceMode.Near);
            }

            if (item.ParentHolder is Pawn_ApparelTracker apparelTracker)
            {
                apparelTracker.Remove((Apparel)item);
            }
            else
            {
                item.holdingOwner?.Remove(item);
            }

            if (!seller.inventory.innerContainer.TryAdd(item))
            {
                GenPlace.TryPlaceThing(item, seller.Position, seller.Map, ThingPlaceMode.Near);
            }

            Messages.Message($"卖出成功：获得了 {price} 金钱", MessageTypeDefOf.PositiveEvent);
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
            RefreshStock();
        }

        private void SpendCoinsFromSeller(ThingDef coinDef, int cost)
        {
            int rem = cost;
            var inv = seller.inventory.innerContainer.Where(t => t.def == coinDef).ToList();
            foreach (var t in inv)
            {
                int takeCount = Mathf.Min(rem, t.stackCount);
                t.stackCount -= takeCount;
                rem -= takeCount;
                if (t.stackCount <= 0)
                {
                    t.Destroy();
                }
                if (rem <= 0) break;
            }
        }
        private void DrawHeader(Rect inRect)
        {
            float tw = 400f, th = 66f;
            Rect tr = new Rect((inRect.width - tw) / 2f, 5f, tw, th);
            GUI.DrawTexture(tr, TitleTex);
            ThingDef cd = ThingDef.Named("Merissu_CustomCoin");
            int count = Mathf.Min(CountAvailableCoins(cd), 999);
            GUI.DrawTexture(new Rect(10f, tr.yMax - 35f, 100f, 100f), MoneyIcon);
            DrawDigitalNumber(new Rect(110f, tr.yMax + 5f, 90f, 32f), count);
            Widgets.DrawLineHorizontal(0f, 105f, inRect.width);
        }

        private void DrawEmptyMsg(Rect inRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            string msg = isSellMode ? "你身上没有可卖出的卡牌" : "商人身上没有可交易的卡牌";
            Widgets.Label(new Rect(0f, 120f, inRect.width, 200f), msg);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawLayerBanner(Thing item, Rect refRect)
        {
            Texture2D bannerTex = null;
            if (item.def.apparel.layers.Any(l => l.defName == "AuxiliaryEquipmentCard")) bannerTex = TexEquip;
            else if (item.def.apparel.layers.Any(l => l.defName == "AuxiliaryAbilityCard")) bannerTex = TexAbility;
            else if (item.def.apparel.layers.Any(l => l.defName == "ActiveItemCard")) bannerTex = TexActive;
            if (bannerTex != null)
            {
                float bannerWidth = refRect.width;
                float bannerHeight = bannerWidth * (2f / 9f);
                GUI.DrawTexture(new Rect(refRect.x, refRect.yMax - 25f, bannerWidth, bannerHeight), bannerTex);
            }
        }

        private void DrawDigitalNumber(Rect rect, int number)
        {
            int h = (number / 100) % 10, t = (number / 10) % 10, u = number % 10;
            float w = 25f, sp = -7f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 25f), GetNumberTex(h));
            GUI.DrawTexture(new Rect(rect.x + w + sp, rect.y, w, 25f), GetNumberTex(t));
            GUI.DrawTexture(new Rect(rect.x + (w + sp) * 2, rect.y, w, 25f), GetNumberTex(u));
        }

        private Texture2D GetNumberTex(int num) => ContentFinder<Texture2D>.Get(NumberPathPrefix + num);

        private void TryPurchaseRealItem(Thing item, int cost)
        {
            ThingDef coinDef = ThingDef.Named("Merissu_CustomCoin");

            if (CountAvailableCoins(coinDef) >= cost)
            {
                SpendCoins(coinDef, cost);

                Thing sellerCoin = ThingMaker.MakeThing(coinDef);
                sellerCoin.stackCount = cost;
                if (!seller.inventory.innerContainer.TryAdd(sellerCoin))
                {
                    GenPlace.TryPlaceThing(sellerCoin, seller.Position, seller.Map, ThingPlaceMode.Near);
                }

                Thing p = item.SplitOff(1);
                GenPlace.TryPlaceThing(p, buyer.Position, buyer.Map, ThingPlaceMode.Near);

                Messages.Message($"购买成功：获得了 {p.LabelCap}", MessageTypeDefOf.PositiveEvent);
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                RefreshStock();
            }
            else
            {
                Messages.Message("金钱不足！", MessageTypeDefOf.RejectInput);
            }
        }
        private int CountAvailableCoins(ThingDef coinDef)
        {
            int c = buyer.inventory.innerContainer.TotalStackCountOfDef(coinDef);
            if (buyer.Map != null)
                foreach (Thing t in buyer.Map.listerThings.ThingsOfDef(coinDef))
                    if (!t.IsForbidden(Faction.OfPlayer)) c += t.stackCount;
            return c;
        }

        private void SpendCoins(ThingDef coinDef, int cost)
        {
            int rem = cost;
            var inv = buyer.inventory.innerContainer.Where(t => t.def == coinDef).ToList();
            foreach (var t in inv) { int tk = Mathf.Min(rem, t.stackCount); t.stackCount -= tk; rem -= tk; if (t.stackCount <= 0) t.Destroy(); if (rem <= 0) return; }
            if (buyer.Map != null && rem > 0)
            {
                var map = buyer.Map.listerThings.ThingsOfDef(coinDef).Where(t => !t.IsForbidden(Faction.OfPlayer)).ToList();
                foreach (var t in map) { int tk = Mathf.Min(rem, t.stackCount); t.stackCount -= tk; rem -= tk; if (t.stackCount <= 0) t.Destroy(); if (rem <= 0) return; }
            }
        }
    }
}