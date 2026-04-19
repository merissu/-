using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Dialog_CardSelection : Window
    {
        private Pawn owner;
        private List<Thing> cards;
        private Action<Thing> onSelected;
        private string title;
        private Vector2 scrollPos;

        private static readonly string[] targetLayers = { "AuxiliaryEquipmentCard", "AuxiliaryAbilityCard", "ActiveItemCard" };

        public override Vector2 InitialSize => new Vector2(500f, 600f);
        public static float GetCustomPrice(Thing thing)
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
        public Dialog_CardSelection(Pawn owner, string title, Action<Thing> onSelected)
        {
            this.owner = owner;
            this.title = title;
            this.onSelected = onSelected;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.layer = WindowLayer.Super;

            List<Thing> potential = new List<Thing>();

            if (owner.inventory?.innerContainer != null)
            {
                potential.AddRange(owner.inventory.innerContainer);
            }

            if (owner.apparel != null)
            {
                potential.AddRange(owner.apparel.WornApparel.Cast<Thing>());
            }

            this.cards = potential.Where(t =>
                t.def.IsApparel &&
                t.def.apparel.layers.Any(l => targetLayers.Contains(l.defName))
            ).ToList();
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 40f), title);
            Widgets.DrawLineHorizontal(0, 45f, inRect.width);

            Rect outRect = new Rect(0, 50f, inRect.width, inRect.height - 110f);
            Rect viewRect = new Rect(0, 0, outRect.width - 16f, cards.Count * 50f);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            for (int i = 0; i < cards.Count; i++)
            {
                Thing card = cards[i];
                Rect rowRect = new Rect(0, i * 50f, viewRect.width, 45f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                float price = GetCustomPrice(card);

                Rect priceRect = new Rect(rowRect.width - 180, rowRect.y + 10, 100, 30);
                GUI.color = Color.cyan; 
                Widgets.Label(priceRect, $"🪙 {price:F1}"); 
                GUI.color = Color.white;
                Widgets.ThingIcon(new Rect(5, rowRect.y + 2, 40, 40), card.def);
                Widgets.Label(new Rect(55, rowRect.y + 10, 300, 30), card.LabelCap);

                if (Widgets.ButtonText(new Rect(rowRect.width - 85, rowRect.y + 5, 80, 35), "选择"))
                {
                    onSelected?.Invoke(card);
                    this.Close();
                }
            }
            Widgets.EndScrollView();

            if (cards.NullOrEmpty())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(outRect, "没有找到可用的卡牌");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2 - 50, inRect.height - 40, 100, 35), "关闭"))
            {
                this.Close();
            }
        }
    }
}