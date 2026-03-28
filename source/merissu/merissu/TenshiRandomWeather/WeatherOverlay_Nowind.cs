using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class NowindWeatherOverlayController : GameComponent
    {
        private const string SpecialVisualWeather = "Nowind";
        private Texture2D dayOverlayTex;
        private Material dayOverlayMat;

        public NowindWeatherOverlayController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            DrawDayOverlay();
        }

        private void DrawDayOverlay()
        {
            Map map = Find.CurrentMap;
            if (map == null || map.weatherManager.curWeather.defName != SpecialVisualWeather) return;

            float glowAlpha = map.skyManager.CurSkyGlow;
            if (glowAlpha <= 0.02f) return;

            if (dayOverlayTex == null)
            {
                dayOverlayTex = ContentFinder<Texture2D>.Get("weather/weatherA000");
                if (dayOverlayTex != null)
                {
                    dayOverlayMat = MaterialPool.MatFrom(dayOverlayTex, ShaderDatabase.MoteGlow, Color.white);
                }
            }

            if (dayOverlayTex != null && dayOverlayMat != null)
            {
                float size = UI.screenHeight;
                Rect rect = new Rect(UI.screenWidth - size, 0f, size, size);
                float finalAlpha = glowAlpha * 0.8f;

                GUI.color = new Color(1f, 1f, 1f, finalAlpha);
                GenUI.DrawTextureWithMaterial(rect, dayOverlayTex, dayOverlayMat);
                GUI.color = Color.white;
            }
        }
    }
}