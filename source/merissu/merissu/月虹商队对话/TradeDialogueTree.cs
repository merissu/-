using System.Collections.Generic;
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

            switch (stage)
            {
                case 0:
                    return new DialogueLine { Speaker = seller, Text = "你好！有什么可以帮忙的吗？", Mood = DialogueMood.Ordinary };

                case 1:
                    return new DialogueLine
                    {
                        Speaker = buyer,
                        Text = "我该怎么开口呢...",
                        Mood = DialogueMood.Think,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption { Text = "我可以用这张卡牌交换你手中的卡牌吗？", NextStage = 10 },
                            new DialogueOption {
                                Text = "(挑衅)识相一点,把你的卡牌交出来",
                                OnSelect = () => { seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -10); },
                                NextStage = 20
                            },
                            new DialogueOption { Text = "我正在执行一个秘密任务,需要你手中的卡牌。", NextStage = 30 }
                        }
                    };

                case 10:
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "你想拿哪张卡来交换？让我看看你的诚意。",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption> {
                            new DialogueOption {
                                Text = "[展示我的卡牌]",
                                OnSelect = () => {
                                    var selWin = new Dialog_CardSelection(buyer, "选择你要拿出的卡牌", (selected) => {
                                        window.playerOfferedCard = selected;
                                        window.dialogueStage = 11; 
                                    });
                                    Find.WindowStack.Add(selWin);
                                }
                            }
                        }
                    };

                case 11: 
                    return new DialogueLine
                    {
                        Speaker = buyer,
                        Text = $"我打算用 【{window.playerOfferedCard.Label}】 交换，你那里有什么？",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption> {
                            new DialogueOption {
                                Text = "[查看商人的货物]",
                                OnSelect = () => {
                                    Find.WindowStack.Add(new Dialog_CardSelection(seller, "选择你想要的卡牌", (selected) => {
                                        window.traderTargetCard = selected;
                                        window.ProcessTradeLogic();
                                    }));
                                }
                            }
                        }
                    };

                case 15: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "哈哈哈！这张卡正是我需要的，你真是个爽快人。这笔交易我成交了！",
                        Mood = DialogueMood.Excitement,
                        Options = new List<DialogueOption> { new DialogueOption { Text = "合作愉快", NextStage = 999 } }
                    };

                case 16: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "哦不，我不会傻到做亏本买卖。",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption
                            {
                                Text = "(武力)我给你一次机会再考虑考虑...",
                                OnSelect = () => {
                                    if (buyerCombat <= sellerCombat) {
                                        seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -2);
                                        Messages.Message($"{seller.LabelShort} 对你的威胁感到不悦。", MessageTypeDefOf.NegativeEvent);
                                    }
                                },
                                NextStage = buyerCombat > sellerCombat ? 101 : 102
                            },
                            new DialogueOption { Text = "(社交)哦，我还以为我们是朋友呢。", NextStage = buyerSocial > sellerSocial ? 201 : 202 },
                            new DialogueOption { Text = "(智识)啊！恶魔！那张卡牌被恶魔诅咒了！", NextStage = buyerIntellectual > sellerIntellectual ? 301 : 305 }
                        }
                    };

                case 101: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "抱歉，我刚才昏了头，你说得对，这笔交易很划算。",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption
                            {
                                Text = "感谢你的慷慨。",
                                OnSelect = () => {
                                    seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -2);
                                    TradeCardUtility.ExecutePhysicalExchange(buyer, seller, window.playerOfferedCard, window.traderTargetCard);
                                },
                                NextStage = 999
                            }
                        }
                    };

                case 102: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "嗯？你是在威胁我？",
                        Mood = DialogueMood.Excitement,
                        Options = new List<DialogueOption> {
                            new DialogueOption {
                                Text = "额...我是说，我可以买下这个卡牌。",
                                NextStage = 103
                            },
                            new DialogueOption {
                                Text = "抱歉大人，我被冲昏了头，请原谅我。",
                                OnSelect = () => {
                                    seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -5);
                                },
                                NextStage = 104
                            }
                        }
                    };

                case 103:
                    float buyPrice = TradeCardUtility.GetCustomCardPrice(window.traderTargetCard) * 2f;
                    bool canAfford = TradeCardUtility.CheckCustomCoinCount(buyer.Map, buyPrice);

                    var options = new List<DialogueOption>();

                    if (canAfford)
                    {
                        options.Add(new DialogueOption
                        {
                            Text = $"[付钱] 支付 {Mathf.CeilToInt(buyPrice)} 枚金钱",
                            OnSelect = () => {
                                if (TradeCardUtility.TryPayWithCustomCoin(buyer.Map, buyPrice))
                                {
                                    TradeCardUtility.ExecutePhysicalExchangeOnlyGet(buyer, window.traderTargetCard);
                                    window.dialogueStage = 999;
                                }
                            }
                        });
                    }
                    else
                    {
                        options.Add(new DialogueOption { Text = $"[钱不够] 需要 {Mathf.CeilToInt(buyPrice)} 枚金钱", NextStage = 103 });
                    }

                    options.Add(new DialogueOption
                    {
                        Text = "太贵了，我再想想",
                        OnSelect = () => {
                            seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -6);
                        },
                        NextStage = 1
                    });

                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = $"想要那张卡？一共要 {Mathf.CeilToInt(buyPrice)} 枚金钱，少一个都不行。",
                        Mood = DialogueMood.Ordinary,
                        Options = options
                    };

                case 104:
                    return new DialogueLine { Speaker = seller, Text = "你最好好好考虑你要说的话，否则我会把你的舌头拔下来。", Mood = DialogueMood.Excitement, NextStage = 999 };

                case 201: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "那还说啥了，我亏点钱呗，卡牌给你了。",
                        Mood = DialogueMood.Touching,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption
                            {
                                Text = "这扯不扯，那卡牌送你了。",
                                OnSelect = () => {
                                    TradeCardUtility.ExecutePhysicalExchange(buyer, seller, window.playerOfferedCard, window.traderTargetCard);
                                },
                                NextStage = 999
                            }
                        }
                    };

                case 202: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "真正的朋友不会对我这么吝啬。",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption
                            {
                                Text = "你说得对，也许我们应该重新考虑一下这笔交易。",
                                OnSelect = () =>
                                {
                                    seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -1);
                                    Messages.Message($"{seller.LabelShort} 觉得你不够慷慨，关系下降了。", MessageTypeDefOf.NegativeEvent);
                                },
                                NextStage = 1
                            }
                        }
                    };

                case 301: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "嘿！不要说这种不吉利的话！",
                        Mood = DialogueMood.Excitement,
                        Options = new List<DialogueOption> { new DialogueOption { Text = "是真的，不瞒你说，我的眼睛被超凡智能开过光...", NextStage = 302 } }
                    };

                case 302:
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "别说了！赶快把这个鬼卡牌拿走！",
                        Mood = DialogueMood.Excitement,
                        Options = new List<DialogueOption>
                        {
                            new DialogueOption
                            {
                                Text = "明智的选择，我就知道你不是蠢人。",
                                OnSelect = () =>
                                {
                                    TradeCardUtility.ExecutePhysicalExchange(buyer, seller, window.playerOfferedCard, window.traderTargetCard);
                                },
                                NextStage = 999
                            }
                        }
                    };

                case 305: 
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "你在胡扯什么？什么年代了还搞这种封建迷信。",
                        Mood = DialogueMood.Ordinary,
                        Options = new List<DialogueOption> { new DialogueOption { Text = "是真的....", NextStage = 306 } }
                    };

                case 306:
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "*打断* 你当我傻吗？如果你再说这些胡话，我就得重新评估交易了。",
                        Mood = DialogueMood.Excitement,
                        Options = new List<DialogueOption> { new DialogueOption { Text = "抱歉..我刚才可能被恶魔附身了。", NextStage = 307 } }
                    };

                case 307:
                    return new DialogueLine
                    {
                        Speaker = seller,
                        Text = "够了！",
                        Mood = DialogueMood.Ordinary,
                        OnSelect = () => {
                            seller.Faction?.TryAffectGoodwillWith(buyer.Faction, -1);
                            Messages.Message($"{seller.LabelShort} 对你的胡言乱语感到厌烦。", MessageTypeDefOf.NegativeEvent);
                        },
                        NextStage = 999
                    };

                case 20:
                    return new DialogueLine { Speaker = seller, Text = "做梦！离我远点！", Mood = DialogueMood.Excitement };

                case 30:
                    float socialSkill = buyer.skills.GetSkill(SkillDefOf.Social).Level;
                    if (socialSkill >= 10)
                    {
                        return new DialogueLine
                        {
                            Speaker = seller,
                            Text = "原来如此...既然是秘密任务，那就请便吧。",
                            Mood = DialogueMood.Touching,
                            Options = new List<DialogueOption> { new DialogueOption { Text = "谢谢。", NextStage = 10 } }
                        };
                    }
                    else
                    {
                        return new DialogueLine { Speaker = seller, Text = "滚蛋！这种借口三岁小孩都不信！", Mood = DialogueMood.Ordinary };
                    }

                default:
                    return null;
            }
        }
    }
}