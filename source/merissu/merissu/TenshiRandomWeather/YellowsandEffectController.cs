using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class YellowsandEffectController : GameComponent
    {
        private const string TargetWeatherDef = "Yellowsand";

        private static readonly Material SandMat = MaterialPool.MatFrom("weather/weatherI000", ShaderDatabase.Mote, Color.white);

        private const float ScrollSpeed = 0.08f;

        private static readonly Color SandTintColor = new Color(0.85f, 0.7f, 0.4f, 0.9f);

        public YellowsandEffectController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            if (Find.CurrentMap?.weatherManager?.curWeather?.defName != TargetWeatherDef) return;

            DrawSandStormOverlay();
        }

        private void DrawSandStormOverlay()
        {
            float drawWidth = UI.screenWidth;
            float aspectRatio = (float)SandMat.mainTexture.height / SandMat.mainTexture.width;
            float drawHeight = drawWidth * aspectRatio;

            float screenY = UI.screenHeight - drawHeight;

            float uvOffset = (RealTime.LastRealTime * ScrollSpeed) % 1f;
            float edgeFix = 0.01f;
            Rect uvRect = new Rect(-uvOffset, edgeFix, 1f, 1f - (edgeFix * 2));

            GUI.color = SandTintColor;
            GUI.DrawTextureWithTexCoords(new Rect(0, screenY, drawWidth, drawHeight), SandMat.mainTexture, uvRect);

            float uvOffsetSecond = (RealTime.LastRealTime * (ScrollSpeed * 0.5f)) % 1f;
            Rect uvRectSecond = new Rect(uvOffsetSecond, edgeFix, 1f, 1f - (edgeFix * 2));

            GUI.color = SandTintColor * 0.5f;
            GUI.DrawTextureWithTexCoords(new Rect(0, screenY, drawWidth, drawHeight), SandMat.mainTexture, uvRectSecond);

            GUI.color = Color.white;
        }
    }
}