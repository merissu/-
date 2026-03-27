using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class KawamuraEffectController : GameComponent
    {
        private const string TargetWeatherDef = "Kawamura";
        private static readonly Material FogMat = MaterialPool.MatFrom("weather/weatherE000", ShaderDatabase.Mote, Color.white);

        private const float ScrollSpeed = 0.05f; 

        public KawamuraEffectController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            if (Find.CurrentMap?.weatherManager?.curWeather?.defName != TargetWeatherDef) return;

            DrawScrollingFog();
        }

        private void DrawScrollingFog()
        {
            float drawWidth = UI.screenWidth;
            float aspectRatio = (float)FogMat.mainTexture.height / FogMat.mainTexture.width;
            float drawHeight = drawWidth * aspectRatio;

            float screenY = UI.screenHeight - drawHeight;

            float uvOffset = (RealTime.LastRealTime * ScrollSpeed) % 1f;
            float edgeFix = 0.01f; 
            Rect uvRect = new Rect(-uvOffset, edgeFix, 1f, 1f - (edgeFix * 2));

            GUI.color = new Color(1f, 1f, 1f, 0.7f); 
            GUI.DrawTextureWithTexCoords(new Rect(0, screenY, drawWidth, drawHeight), FogMat.mainTexture, uvRect);
            GUI.color = Color.white;
        }
    }
}