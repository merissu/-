using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public static class TradeDialogueTree
    {
        public static DialogueLine GetDialogueData(int stage, Dialog_TradeTalk window)
        {
            Pawn buyer = window.buyer;
            Pawn seller = window.seller;

            float buyerCombat = buyer.skills.GetSkill(SkillDefOf.Shooting).Level + buyer.skills.GetSkill(SkillDefOf.Melee).Level;
            float sellerCombat = seller.skills.GetSkill(SkillDefOf.Shooting).Level + seller.skills.GetSkill(SkillDefOf.Melee).Level;
            float buyerSocial = buyer.skills.GetSkill(SkillDefOf.Social).Level;
            float sellerSocial = seller.skills.GetSkill(SkillDefOf.Social).Level;
            float buyerIntellectual = buyer.skills.GetSkill(SkillDefOf.Intellectual).Level;
            float sellerIntellectual = seller.skills.GetSkill(SkillDefOf.Intellectual).Level;

            var nodes = DefDatabase<DialogueNodeDef>.AllDefsListForReading.Where(x => x.stage == stage).ToList();
            if (nodes.Count == 0) return null;

            DialogueNodeDef nodeDef = nodes.Count > 1
                ? (nodes.FirstOrDefault(x => x.requireSocial > 0 && buyerSocial >= x.requireSocial) ?? nodes.FirstOrDefault(x => x.requireSocial == 0))
                : nodes[0];

            if (nodeDef == null) return null;

            if (window.lastProcessedStage != stage)
            {
                if (nodeDef.texts != null && nodeDef.texts.Count > 0)
                    window.cachedBaseText = nodeDef.texts.RandomElement();
                else
                    window.cachedBaseText = nodeDef.text ?? "";

                window.cachedOptionTexts.Clear();

                if (nodeDef.options != null)
                {
                    foreach (var opt in nodeDef.options)
                    {
                        string oText = (opt.texts != null && opt.texts.Count > 0)
                            ? opt.texts.RandomElement()
                            : (opt.text ?? "Missing Option Text");

                        if (oText.Contains("{Chance}"))
                        {
                            float bHand = buyer.skills.GetSkill(SkillDefOf.Crafting).Level;
                            float sHand = seller.skills.GetSkill(SkillDefOf.Crafting).Level;
                            float chance = bHand < sHand ? 0f : Mathf.Clamp(0.30f + (bHand - sHand) * 0.05f, 0f, 0.95f);
                            oText = oText.Replace("{Chance}", (chance * 100f).ToString("F0") + "%");
                        }
                        if (oText.Contains("{Difficulty}"))
                        {
                            bool isEasy = false;
                            switch (opt.skillCheck)
                            {
                                case SkillCompareType.Combat: isEasy = buyerCombat > sellerCombat; break;
                                case SkillCompareType.Social: isEasy = buyerSocial > sellerSocial; break;
                                case SkillCompareType.Intellectual: isEasy = buyerIntellectual > sellerIntellectual; break;
                                default: isEasy = true; break;
                            }
                            string diffStr = isEasy ? "<color=#66E066>容易</color>" : "<color=#E06666>非常困难</color>";
                            oText = oText.Replace("{Difficulty}", diffStr);
                        }

                        window.cachedOptionTexts.Add(oText);
                    }
                }

                window.lastProcessedStage = stage;
            }
            string finalText = window.cachedBaseText;

            if (stage == 11 && window.playerOfferedCard != null)
            {
                finalText = finalText.Replace("{0}", window.playerOfferedCard.Label);
            }

            float buyPrice = 0f;
            if (stage == 103 && window.traderTargetCard != null)
            {
                buyPrice = TradeCardUtility.GetCustomCardPrice(window.traderTargetCard) * 2f;
                finalText = finalText.Replace("{0}", Mathf.CeilToInt(buyPrice).ToString());
            }

            DialogueLine line = new DialogueLine
            {
                Speaker = nodeDef.speaker == DialogueSpeaker.Buyer ? buyer : seller,
                Text = finalText,
                Mood = nodeDef.mood,
                Options = new List<DialogueOption>()
            };

            if (nodeDef.defaultNextStage != -1) line.NextStage = nodeDef.defaultNextStage;

            if (nodeDef.onLineComplete != null)
            {
                line.OnSelect = () => ExecuteEvent(nodeDef.onLineComplete, window, buyer, seller);
            }

            if (nodeDef.options != null)
            {
                for (int i = 0; i < nodeDef.options.Count; i++)
                {
                    var optDef = nodeDef.options[i];
                    string currentOptText = (window.cachedOptionTexts.Count > i) ? window.cachedOptionTexts[i] : (optDef.text ?? "");

                    DialogueOption option = new DialogueOption { Text = currentOptText };

                    if (optDef.skillCheck == SkillCompareType.None && optDef.onSelect?.customAction == "CheckAfford")
                    {
                        bool canAfford = TradeCardUtility.CheckCustomCoinCount(buyer.Map, buyPrice);
                        if (canAfford)
                        {
                            option.Text = $"[付钱] 支付 {Mathf.CeilToInt(buyPrice)} 枚金钱";
                            option.OnSelect = () => {
                                if (TradeCardUtility.TryPayWithCustomCoin(buyer.Map, buyPrice))
                                {
                                    TradeCardUtility.ExecutePhysicalExchangeOnlyGet(buyer, window.traderTargetCard);
                                    window.dialogueStage = 999;
                                }
                            };
                            option.NextStage = 999;
                        }
                        else
                        {
                            option.Text = $"[钱不够] 需要 {Mathf.CeilToInt(buyPrice)} 枚金钱";
                            option.NextStage = 103;
                        }
                    }
                    else if (optDef.skillCheck != SkillCompareType.None)
                    {
                        bool success = false;
                        switch (optDef.skillCheck)
                        {
                            case SkillCompareType.Combat: success = buyerCombat > sellerCombat; break;
                            case SkillCompareType.Social: success = buyerSocial > sellerSocial; break;
                            case SkillCompareType.Intellectual: success = buyerIntellectual > sellerIntellectual; break;
                        }

                        if (success)
                        {
                            option.NextStage = (optDef.randomSuccessStages != null && optDef.randomSuccessStages.Count > 0)
                                ? optDef.randomSuccessStages.RandomElement()
                                : optDef.successStage;
                        }
                        else
                        {
                            option.NextStage = (optDef.randomFailStages != null && optDef.randomFailStages.Count > 0)
                                ? optDef.randomFailStages.RandomElement()
                                : optDef.failStage;
                        }
                        option.OnSelect = () => {
                            if (optDef.onSelect != null) ExecuteEvent(optDef.onSelect, window, buyer, seller);
                            if (success && optDef.onSuccess != null) ExecuteEvent(optDef.onSuccess, window, buyer, seller);
                            if (!success && optDef.onFail != null) ExecuteEvent(optDef.onFail, window, buyer, seller);
                        };
                    }
                    else
                    {
                        option.NextStage = (optDef.nextStage == -1) ? stage : optDef.nextStage;

                        if (optDef.onSelect != null)
                        {
                            option.OnSelect = () => ExecuteEvent(optDef.onSelect, window, buyer, seller);
                        }
                    }

                    line.Options.Add(option);
                }
            }

            return line;
        }
        private static void FailSteal(Dialog_TradeTalk window, Pawn buyer, Pawn seller)
        {
            Messages.Message("偷窃失败！" + seller.LabelShort + " 发现了你的小动作！", MessageTypeDefOf.RejectInput);
            seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -30);
            window.dialogueStage = 30; 
        }
        private static void ExecuteEvent(DialogueEventDef ev, Dialog_TradeTalk window, Pawn buyer, Pawn seller)
        {
            if (ev.goodwillChange != 0)
            {
                seller.Faction?.TryAffectGoodwillWith(buyer.Faction, ev.goodwillChange);
            }
            if (!string.IsNullOrEmpty(ev.messageText))
            {
                Messages.Message(ev.messageText.Replace("{SellerLabel}", seller.LabelShort), ev.messageType ?? MessageTypeDefOf.NeutralEvent);
            }
            if (!string.IsNullOrEmpty(ev.customAction))
            {
                switch (ev.customAction)
                {
                    case "OpenPlayerCardSelect":
                        Find.WindowStack.Add(new Dialog_CardSelection(buyer, "选择你要拿出的卡牌", (selected) => {
                            window.playerOfferedCard = selected;
                            window.dialogueStage = 11;
                        }));
                        break;
                    case "OpenTraderCardSelect":
                        Find.WindowStack.Add(new Dialog_CardSelection(seller, "选择你想要的卡牌", (selected) => {
                            window.traderTargetCard = selected;
                            window.ProcessTradeLogic();
                        }));
                        break;
                    case "ExecuteExchange":
                        TradeCardUtility.ExecutePhysicalExchange(buyer, seller, window.playerOfferedCard, window.traderTargetCard);
                        break;
                    case "OpenStealSelect":
                        Find.WindowStack.Add(new Dialog_CardSelection(seller, "选择你要窃取的卡牌", (selected) => {
                            Texture2D targetTex = selected.def.uiIcon;

                            float bHand = buyer.skills.GetSkill(SkillDefOf.Crafting).Level; 
                            float sHand = seller.skills.GetSkill(SkillDefOf.Crafting).Level;

                            float chance = bHand < sHand ? 0f : Mathf.Clamp(0.30f + (bHand - sHand) * 0.05f, 0f, 0.95f);
                            float diff = Mathf.Max(0, sHand - bHand);

                            Find.WindowStack.Add(new Dialog_ThreeCardMonte(chance, selected.def.uiIcon, (bool isWin) => {
                                if (isWin)
                                {
                                    Messages.Message("窃取成功！你神不知鬼不觉地拿走了 " + selected.LabelShort, MessageTypeDefOf.PositiveEvent);
                                    TradeCardUtility.ExecutePhysicalExchangeOnlyGet(buyer, selected);
                                    window.lastProcessedStage = -1;
                                    window.dialogueStage = 999;
                                }
                                else
                                {
                                    window.lastProcessedStage = -1;
                                    FailSteal(window, buyer, seller); 
                                }
                            }));
                        }));
                        break;
                }
            }
        }
    }
}