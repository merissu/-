using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class WeatherBackdropScroller : GameComponent
    {
        private const string TargetWeatherDef = "Qinglan";
        private const string TexturePath = "weather/weatherJ000";

        private float loopDuration = 1.8f;      
        private float rotationAngle = -20f;    
        private float textureScale = 2.5f;     
        private float transparency = 0.4f;     

        private Texture2D scrollTex;

        public WeatherBackdropScroller(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            Map map = Find.CurrentMap;
            if (map == null || map.weatherManager.curWeather.defName != TargetWeatherDef) return;

            if (scrollTex == null)
            {
                scrollTex = ContentFinder<Texture2D>.Get(TexturePath);
                if (scrollTex == null) return;
            }

            float progress = (Time.time % loopDuration) / loopDuration;

            DrawScrollingTexture(progress);
        }

        private void DrawScrollingTexture(float p)
        {
            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;

            float texAspect = (float)scrollTex.width / scrollTex.height;
            float drawHeight = screenH * textureScale;
            float drawWidth = drawHeight * texAspect;

            float xBuffer = drawWidth * 0.8f;

            float startX = -xBuffer;
            float startY = screenH * 0.2f;

            float endX = screenW + xBuffer;
            float endY = screenH * -0.8f;

            float curX = Mathf.Lerp(startX, endX, p);
            float curY = Mathf.Lerp(startY, endY, p);

            Rect rect = new Rect(curX, curY, drawWidth, drawHeight);

            Matrix4x4 savedMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(rotationAngle, rect.center);

            GUI.color = new Color(1f, 1f, 1f, transparency);
            GUI.DrawTexture(rect, scrollTex, ScaleMode.StretchToFill);

            GUI.color = Color.white;
            GUI.matrix = savedMatrix;
        }
    }
}