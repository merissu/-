using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Dialog_ThreeCardMonte : Window
    {
        public enum MonteState { Prepare, Anticipation, Shuffling, WaitingInput, RevealChoice, RevealAll }

        private MonteState state = MonteState.Prepare;
        private int playerChoice = -1;
        private float stateTimer;
        private Action<bool> onFinished;

        private static readonly Vector2 CardSize = new Vector2(100f, 100f);
        private const int CardsPerRow = 10;
        private const float HorizontalSpacing = 110f;
        private const float VerticalSpacing = 120f;

        private List<Vector2> renderPositions = new List<Vector2>();
        private List<Texture2D> cardTextures = new List<Texture2D>();
        private Texture2D backTex;

        private float shuffleTimeLeft;
        private float initialShuffleTime; 
        private List<int> currentMapping = new List<int>();
        private int swapA = -1, swapB = -1;
        private float targetSwapDuration; 
        private const float StartSwapDuration = 0.6f; 
        private int totalCards;

        public override Vector2 InitialSize
        {
            get
            {
                int rows = Mathf.CeilToInt((float)totalCards / CardsPerRow);
                float width = Mathf.Min(totalCards, CardsPerRow) * HorizontalSpacing + 100f;
                float height = 250f + (rows * VerticalSpacing);
                return new Vector2(Mathf.Max(750f, width), height);
            }
        }

        public Dialog_ThreeCardMonte(float successChance, Texture2D targetCardTex, Action<bool> callback)
        {
            this.onFinished = callback;
            successChance = Mathf.Clamp(successChance, 0f, 1f);

            float inverse = 1f - successChance;
            float steps = inverse / 0.05f;

            this.totalCards = 5 + Mathf.RoundToInt(steps);

            this.initialShuffleTime = 2f + (steps * 0.25f);
            this.shuffleTimeLeft = this.initialShuffleTime;

            this.targetSwapDuration = Mathf.Max(0.5f - (steps * 0.02f), 0.08f);

            this.stateTimer = 2.0f; 
            this.doCloseX = false;
            this.closeOnClickedOutside = false;
            this.layer = WindowLayer.Super;

            backTex = ContentFinder<Texture2D>.Get("UI/BACK_max");
            LoadCardsDynamically(targetCardTex);

            currentMapping.Clear();
            renderPositions.Clear();
            for (int i = 0; i < totalCards; i++)
            {
                currentMapping.Add(i);
                renderPositions.Add(GetSlotPos(i));
            }
        }

        private void LoadCardsDynamically(Texture2D targetTex)
        {
            cardTextures.Clear();
            cardTextures.Add(targetTex);

            var pool = ContentFinder<Texture2D>.GetAllInFolder("Accessory/card")
                .Where(t => t != targetTex && !IsBlacklisted(t.name))
                .ToList();

            int needed = totalCards - 1;
            for (int i = 0; i < needed; i++)
            {
                if (pool.Count > 0)
                {
                    var pick = pool.RandomElement();
                    cardTextures.Add(pick);
                    pool.Remove(pick);
                }
                else cardTextures.Add(backTex);
            }
            this.totalCards = cardTextures.Count;
        }

        private bool IsBlacklisted(string name)
        {
            string last = name.Split('/').Last();
            return new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "title", "AuxiliaryEquipmentCard", "AuxiliaryAbilityCard", "ActiveItemCard", "Merissu_CustomCoin", "money" }.Contains(last);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;

            string label = "";
            switch (state)
            {
                case MonteState.Prepare: label = "记好你的目标卡牌！"; break;
                case MonteState.Anticipation: label = "准备好了吗？"; break;
                case MonteState.Shuffling: label = "盯紧它！"; break;
                case MonteState.WaitingInput: label = "哪一张才是刚才的目标？"; break;
                case MonteState.RevealChoice: label = "结果是..."; break;
                default: label = "真相大白"; break;
            }
            Widgets.Label(new Rect(0, 20, inRect.width, 40), label);

            UpdateState();

            for (int i = 0; i < totalCards; i++)
            {
                Rect r = new Rect(renderPositions[i], CardSize);
                Texture2D tex = GetCardTextureForState(i);

                if (state == MonteState.Prepare && currentMapping[i] == 0)
                {
                    Widgets.DrawBoxSolidWithOutline(r.ExpandedBy(3f), Color.clear, Color.green, 3);
                }

                if (state == MonteState.WaitingInput)
                {
                    if (Widgets.ButtonImage(r, tex))
                    {
                        playerChoice = i;
                        state = MonteState.RevealChoice;
                        stateTimer = 1.0f;
                    }
                }
                else
                {
                    GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
                }
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private Texture2D GetCardTextureForState(int index)
        {
            if (state == MonteState.Prepare || state == MonteState.Anticipation || state == MonteState.RevealAll)
                return cardTextures[currentMapping[index]];
            if (state == MonteState.RevealChoice && index == playerChoice)
                return cardTextures[currentMapping[index]];
            return backTex;
        }

        private void UpdateState()
        {
            float dt = Time.deltaTime;
            stateTimer -= dt;

            switch (state)
            {
                case MonteState.Prepare:
                    if (stateTimer <= 0)
                    {
                        state = MonteState.Anticipation;
                        stateTimer = 1.0f; 
                    }
                    break;
                case MonteState.Anticipation:
                    if (stateTimer <= 0)
                    {
                        state = MonteState.Shuffling;
                    }
                    break;
                case MonteState.Shuffling:
                    UpdateShuffle(dt);
                    if (shuffleTimeLeft <= 0 && swapA == -1) state = MonteState.WaitingInput;
                    break;
                case MonteState.RevealChoice:
                    if (stateTimer <= 0) { state = MonteState.RevealAll; stateTimer = 1.5f; }
                    break;
                case MonteState.RevealAll:
                    if (stateTimer <= 0)
                    {
                        if (onFinished != null)
                        {
                            var action = onFinished;
                            onFinished = null; 
                            action.Invoke(playerChoice != -1 && currentMapping[playerChoice] == 0);
                        }
                        Close();
                    }
                    break;
            }
        }
        private class SwapTask
        {
            public int a, b;
            public Vector2 startA, startB;
            public float side; 
        }
        private List<SwapTask> activeSwaps = new List<SwapTask>();
        private int lastSwapA = -1;
        private int lastSwapB = -1;
        private int shuffleCount = 0;
        private void UpdateShuffle(float dt)
        {
            if (activeSwaps.Count == 0 && shuffleTimeLeft > 0)
            {
                float progress = Mathf.Clamp01(1f - (shuffleTimeLeft / initialShuffleTime));
                float currentSwapDuration = Mathf.Lerp(StartSwapDuration, targetSwapDuration, progress);
                currentActiveSwapDuration = currentSwapDuration;
                stateTimer = currentSwapDuration;

                int targetSlot = -1;
                for (int i = 0; i < totalCards; i++)
                {
                    if (currentMapping[i] == 0) { targetSlot = i; break; }
                }

                int mainA = Rand.Range(0, totalCards);
                int mainB = (mainA + Rand.Range(1, totalCards)) % totalCards;

                if (shuffleCount == 0)
                {
                    while (mainA == targetSlot || mainB == targetSlot)
                    {
                        mainA = Rand.Range(0, totalCards);
                        mainB = (mainA + Rand.Range(1, totalCards)) % totalCards;
                    }
                }

                activeSwaps.Add(new SwapTask { a = mainA, b = mainB, startA = GetSlotPos(mainA), startB = GetSlotPos(mainB), side = 1f });

                if (mainA != targetSlot && mainB != targetSlot && shuffleCount > 0)
                {
                    int extraB = Rand.Range(0, totalCards);
                    while (extraB == targetSlot || extraB == mainA || extraB == mainB)
                    {
                        extraB = (extraB + 1) % totalCards;
                    }
                    activeSwaps.Add(new SwapTask { a = targetSlot, b = extraB, startA = GetSlotPos(targetSlot), startB = GetSlotPos(extraB), side = -1f });
                }
            }

            if (activeSwaps.Count > 0)
            {
                float rawPercent = Mathf.Clamp01(1f - (stateTimer / currentActiveSwapDuration));
                float percent = Mathf.SmoothStep(0f, 1f, rawPercent);

                foreach (var swap in activeSwaps)
                {
                    float dist = Vector2.Distance(swap.startA, swap.startB);
                    float arc = Mathf.Sin(rawPercent * Mathf.PI) * Mathf.Min(dist * 0.3f, 50f) * swap.side;

                    renderPositions[swap.a] = Vector2.Lerp(swap.startA, swap.startB, percent) + new Vector2(0, arc);
                    renderPositions[swap.b] = Vector2.Lerp(swap.startB, swap.startA, percent) + new Vector2(0, -arc);
                }

                if (rawPercent >= 1f)
                {
                    foreach (var swap in activeSwaps)
                    {
                        int temp = currentMapping[swap.a];
                        currentMapping[swap.a] = currentMapping[swap.b];
                        currentMapping[swap.b] = temp;

                        renderPositions[swap.a] = GetSlotPos(swap.a);
                        renderPositions[swap.b] = GetSlotPos(swap.b);
                    }

                    shuffleTimeLeft -= currentActiveSwapDuration;
                    shuffleCount++;
                    activeSwaps.Clear();
                }
            }
        }
        private float currentActiveSwapDuration; 

        private Vector2 GetSlotPos(int index)
        {
            int row = index / CardsPerRow;
            int col = index % CardsPerRow;
            int cardsInThisRow = (row < (totalCards / CardsPerRow)) ? CardsPerRow : (totalCards % CardsPerRow);
            if (cardsInThisRow == 0) cardsInThisRow = CardsPerRow;

            float rowWidth = (cardsInThisRow - 1) * HorizontalSpacing;
            float startX = (InitialSize.x - 100f - rowWidth) / 2f;
            float startY = 80f;

            return new Vector2(startX + col * HorizontalSpacing, startY + row * VerticalSpacing);
        }
    }
}