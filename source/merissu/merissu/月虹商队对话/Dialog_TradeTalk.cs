using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace merissu
{
    public class Dialog_TradeTalk : Window
    {
        public Pawn buyer;
        public Pawn seller;
        public int dialogueStage = 0;
        public Thing playerOfferedCard;
        public Thing traderTargetCard;
        public int lastProcessedStage = -1;
        public string cachedBaseText;
        public List<string> cachedOptionTexts = new List<string>();
        private static readonly Texture2D CustomBgTex = ContentFinder<Texture2D>.Get("UI/stage05a");
        private static readonly Dictionary<DialogueMood, string> MoodPathBase = new Dictionary<DialogueMood, string>
        {
            { DialogueMood.Ordinary, "UI/speechbubbles/Ordinary" },
            { DialogueMood.Excitement, "UI/speechbubbles/Excitement" },
            { DialogueMood.Think, "UI/speechbubbles/Think" },
            { DialogueMood.Touching, "UI/speechbubbles/Touching" }
        };

        private static readonly Dictionary<Pawn, RenderTexture> PortraitCache = new Dictionary<Pawn, RenderTexture>();

        private Vector2 currentLeftPos;
        private Vector2 currentRightPos;
        private float currentLeftScale = 1f;
        private float currentRightScale = 1f;
        private float currentLeftAlpha = 1f;
        private float currentRightAlpha = 1f;

        private const float MoveSpeed = 10f;
        private const float ScaleSpeed = 8f;

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);
        protected override float Margin => 0f;

        public Dialog_TradeTalk(Pawn buyer, Pawn seller)
        {
            this.buyer = buyer;
            this.seller = seller;

            this.forcePause = true;
            this.doCloseX = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.layer = WindowLayer.Super;
            this.doWindowBackground = false;
            this.preventCameraMotion = true;

            float sw = UI.screenWidth;
            float sh = UI.screenHeight;
            float baseH = sh * 1.15f;
            float baseW = baseH * 0.75f;

            currentLeftPos = new Vector2(sw * 0.18f - baseW / 2f, sh - baseH + 50f);
            currentRightPos = new Vector2(sw * 0.82f - baseW / 2f, sh - baseH + 50f);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0, 0, UI.screenWidth, UI.screenHeight);
            if (CustomBgTex != null)
            {
                GUI.DrawTexture(viewRect, CustomBgTex, ScaleMode.ScaleAndCrop);
            }

            var dialogueData = TradeDialogueTree.GetDialogueData(dialogueStage, this);
            if (dialogueData == null)
            {
                this.Close();
                return;
            }

            UpdateAnimation(viewRect, dialogueData.Speaker, dialogueData.IsBoth);

            float basePawnHeight = viewRect.height * 1.15f;
            float basePawnWidth = basePawnHeight * 0.75f;
            DrawManagedPortrait(buyer, currentLeftPos, basePawnWidth, basePawnHeight, currentLeftScale, currentLeftAlpha);
            DrawManagedPortrait(seller, currentRightPos, basePawnWidth, basePawnHeight, currentRightScale, currentRightAlpha);

            DrawDialogueUI(viewRect, dialogueData.Text, dialogueData.Mood, dialogueData.Speaker);

            if (dialogueData.Options != null && dialogueData.Options.Count > 0)
            {
                float optionWidth = 450f;
                float optionHeight = 50f;
                float startY = viewRect.height * 0.65f;

                for (int i = 0; i < dialogueData.Options.Count; i++)
                {
                    var opt = dialogueData.Options[i];
                    Rect btnRect = new Rect((viewRect.width - optionWidth) / 2f, startY + (i * (optionHeight + 12f)), optionWidth, optionHeight);

                    if (opt.Text.Contains("钱不够"))
                    {
                        GUI.color = Color.gray;
                        Widgets.Label(btnRect, $"<color=red>{opt.Text}</color>");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        if (Widgets.ButtonText(btnRect, opt.Text))
                        {
                            opt.OnSelect?.Invoke();
                            dialogueStage = opt.NextStage;
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                    }
                }
            }
            else
            {
                if (Widgets.ButtonInvisible(viewRect))
                {
                    dialogueData.OnSelect?.Invoke();

                    if (dialogueData.NextStage != -1)
                    {
                        dialogueStage = dialogueData.NextStage;
                    }
                    else
                    {
                        dialogueStage++;
                    }
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }
        }

        public void ProcessTradeLogic()
        {
            if (playerOfferedCard == null || traderTargetCard == null) return;

            float playerValue = TradeCardUtility.GetCustomCardPrice(playerOfferedCard);
            float traderValue = TradeCardUtility.GetCustomCardPrice(traderTargetCard);

            if (playerValue >= traderValue)
            {
                TradeCardUtility.ExecutePhysicalExchange(buyer, seller, playerOfferedCard, traderTargetCard);

                if (seller.Faction != null)
                {
                    seller.Faction.TryAffectGoodwillWith(buyer.Faction, 2);
                }
                this.dialogueStage = 15;
            }
            else
            {
                this.dialogueStage = 16;
            }
        }


        private void DrawDialogueUI(Rect screenRect, string text, DialogueMood mood, Pawn currentSpeaker)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = true;

            GUIStyle textStyle = new GUIStyle(Text.CurFontStyle);
            Texture2D bubbleTex = ContentFinder<Texture2D>.Get(MoodPathBase[mood] + "B");

            float maxTextWidth = 500f;
            float calculatedTextHeight = textStyle.CalcHeight(new GUIContent(text), maxTextWidth);
            float bHeight = Mathf.Max(240f, calculatedTextHeight + 100f);

            Vector2 textSizeRaw = textStyle.CalcSize(new GUIContent(text));
            float bWidth = Mathf.Clamp(textSizeRaw.x + 120f, 250f, maxTextWidth + 120f);

            Rect bubbleRect = new Rect(0, 0, bWidth, bHeight);

            if (currentSpeaker == buyer || currentSpeaker == null)
            {
                bubbleRect.x = currentLeftPos.x + (screenRect.width * 0.3f);
                bubbleRect.y = currentLeftPos.y + (screenRect.height * 0.45f);
            }
            else
            {
                bubbleRect.x = currentRightPos.x - bubbleRect.width + (screenRect.width * 0.2f);
                bubbleRect.y = currentRightPos.y + (screenRect.height * 0.5f);
            }

            GUI.color = Color.white;
            if (bubbleTex != null)
            {
                DrawCustomSlicedBubble(bubbleRect, bubbleTex, mood, (currentSpeaker == seller));
            }

            GUI.color = Color.black;
            Rect textRect = bubbleRect.ContractedBy(35f);
            GUI.Label(textRect, text, textStyle);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawCustomSlicedBubble(Rect rect, Texture2D tex, DialogueMood mood, bool flip)
        {
            float texW = tex.width;
            float leftPx, rightPx;
            bool useTiling = false;

            switch (mood)
            {
                case DialogueMood.Ordinary: leftPx = 100f; rightPx = 110f; break;
                case DialogueMood.Excitement: leftPx = 76f; rightPx = 133f; useTiling = true; break;
                case DialogueMood.Think: leftPx = 96f; rightPx = 105f; break;
                default: leftPx = 100f; rightPx = 105f; break;
            }

            float u1 = leftPx / texW;
            float u2 = rightPx / texW;

            if (flip)
            {
                Matrix4x4 savedMatrix = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), rect.center);
                DrawThreeSection(rect, tex, u1, u2, useTiling);
                GUI.matrix = savedMatrix;
            }
            else
            {
                DrawThreeSection(rect, tex, u1, u2, useTiling);
            }
        }

        private void DrawThreeSection(Rect rect, Texture2D tex, float u1, float u2, bool tiling)
        {
            float texW = tex.width;
            float sampleEpsilon = 0.5f / texW;

            float leftWidthUI = u1 * texW;
            float rightWidthUI = (1f - u2) * texW;
            float middleWidthUI = rect.width - leftWidthUI - rightWidthUI;

            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, leftWidthUI, rect.height), tex, new Rect(0, 0, u1, 1));

            Rect midRect = new Rect(rect.x + leftWidthUI, rect.y, middleWidthUI, rect.height);
            if (tiling && middleWidthUI > 0)
            {
                float sourcePxWidth = (u2 - u1) * texW;
                float currentX = midRect.x;
                float remainingWidth = midRect.width;

                while (remainingWidth > 0)
                {
                    float drawWidth = Mathf.Min(remainingWidth, sourcePxWidth);
                    float ratio = drawWidth / sourcePxWidth;
                    float safeU1 = u1 + sampleEpsilon;
                    float totalU = (u2 - u1) - (sampleEpsilon * 2f);
                    float currentUWidth = totalU * ratio;

                    float overlap = (remainingWidth > sourcePxWidth) ? 1.0f : 0f;
                    GUI.DrawTextureWithTexCoords(new Rect(currentX, midRect.y, drawWidth + overlap, rect.height), tex,
                        new Rect(safeU1, 0, currentUWidth, 1));

                    currentX += drawWidth;
                    remainingWidth -= drawWidth;
                }
            }
            else
            {
                GUI.DrawTextureWithTexCoords(midRect, tex, new Rect(u1 + sampleEpsilon, 0, (u2 - u1) - (sampleEpsilon * 2f), 1));
            }

            GUI.DrawTextureWithTexCoords(new Rect(rect.x + leftWidthUI + middleWidthUI, rect.y, rightWidthUI, rect.height), tex, new Rect(u2, 0, 1f - u2, 1));
        }

        private void UpdateAnimation(Rect viewRect, Pawn currentSpeaker, bool isBothSpeaking)
        {
            float basePawnHeight = viewRect.height * 1.15f;
            float basePawnWidth = basePawnHeight * 0.75f;

            float moveDistLeft = viewRect.width * 0.007f;
            float moveDistRight = viewRect.width * 0.05f;
            float dropDist = viewRect.height * 0.04f;
            float inactiveScaleTarget = 0.9f;

            Vector2 leftBase = new Vector2(viewRect.width * 0.18f - basePawnWidth / 2f, viewRect.height - basePawnHeight + 50f);
            bool leftInactive = (currentSpeaker != buyer && currentSpeaker != null && !isBothSpeaking);
            Vector2 targetLeft = leftInactive ? new Vector2(leftBase.x - moveDistLeft, leftBase.y + dropDist) : leftBase;

            currentLeftPos = Vector2.Lerp(currentLeftPos, targetLeft, Time.deltaTime * MoveSpeed);
            currentLeftScale = Mathf.Lerp(currentLeftScale, leftInactive ? inactiveScaleTarget : 1f, Time.deltaTime * ScaleSpeed);
            currentLeftAlpha = Mathf.Lerp(currentLeftAlpha, leftInactive ? 0.4f : 1f, Time.deltaTime * ScaleSpeed);

            Vector2 rightBase = new Vector2(viewRect.width * 0.82f - basePawnWidth / 2f, viewRect.height - basePawnHeight + 50f);
            bool rightInactive = (currentSpeaker != seller && currentSpeaker != null && !isBothSpeaking);
            Vector2 targetRight = rightInactive ? new Vector2(rightBase.x + moveDistRight, rightBase.y + dropDist) : rightBase;

            currentRightPos = Vector2.Lerp(currentRightPos, targetRight, Time.deltaTime * MoveSpeed);
            currentRightScale = Mathf.Lerp(currentRightScale, rightInactive ? inactiveScaleTarget : 1f, Time.deltaTime * ScaleSpeed);
            currentRightAlpha = Mathf.Lerp(currentRightAlpha, rightInactive ? 0.4f : 1f, Time.deltaTime * ScaleSpeed);
        }

        private void DrawManagedPortrait(Pawn pawn, Vector2 pos, float w, float h, float scale, float alpha)
        {
            GUI.color = new Color(alpha, alpha, alpha, alpha >= 1f ? 1f : 0.8f);
            Rect rect = new Rect(pos.x, pos.y, w * scale, h * scale);
            RenderTexture portrait = GetCachedPortrait(pawn, new Vector2(512f, 512f));
            GUI.DrawTexture(rect, portrait, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
        }

        private static RenderTexture GetCachedPortrait(Pawn pawn, Vector2 size)
        {
            if (!PortraitCache.TryGetValue(pawn, out RenderTexture renderTexture) || renderTexture.width != (int)size.x)
            {
                renderTexture = PortraitsCache.Get(pawn, size, Rot4.South, Vector3.zero, 1.25f);
                PortraitCache[pawn] = renderTexture;
            }
            return renderTexture;
        }
    }
}