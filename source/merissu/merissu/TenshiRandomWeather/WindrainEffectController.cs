using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class WindrainEffectController : GameComponent
    {
        private const string TargetWeatherDef = "Windrain";
        private const float CYCLE_DURATION = 1.5f; 

        private List<Texture2D> textures = new List<Texture2D>();
        private bool initialized = false;

        public WindrainEffectController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            if (Find.CurrentMap?.weatherManager?.curWeather?.defName != TargetWeatherDef) return;

            if (!initialized) InitResources();
            DrawFastDiagonalTextures();
        }

        private void InitResources()
        {
            Texture2D texD = ContentFinder<Texture2D>.Get("weather/weatherD000", false);
            Texture2D texB = ContentFinder<Texture2D>.Get("weather/weatherB000", false);
            if (texD != null) textures.Add(texD);
            if (texB != null) textures.Add(texB);
            initialized = true;
        }

        private void DrawFastDiagonalTextures()
        {
            if (textures.Count == 0) return;

            float time = RealTime.LastRealTime;
            for (int i = 0; i < textures.Count; i++)
            {
                float phase = (time + (i * (CYCLE_DURATION / (float)textures.Count))) % CYCLE_DURATION;
                float progress = phase / CYCLE_DURATION;

                DrawSingleFastDiagonal(textures[i], progress);
            }
        }

        private void DrawSingleFastDiagonal(Texture2D tex, float progress)
        {
            float screenDiagonal = Mathf.Sqrt(Mathf.Pow(UI.screenWidth, 2) + Mathf.Pow(UI.screenHeight, 2));
            float drawW = screenDiagonal * 1.5f;

            float originalAspect = (float)tex.height / tex.width; 

            float drawH = drawW / originalAspect;

            Matrix4x4 savedMatrix = GUI.matrix;
            Color savedColor = GUI.color;

            Vector2 center = new Vector2(UI.screenWidth / 2f, UI.screenHeight / 2f);

            float pathAngle = Mathf.Atan2(UI.screenHeight, UI.screenWidth) * Mathf.Rad2Deg;

            float curX = Mathf.Lerp(drawW, -drawW, progress);


            Matrix4x4 globalRotateAndTranslate = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, -pathAngle), Vector3.one);
            Matrix4x4 move = Matrix4x4.Translate(new Vector3(curX, 0, 0));

            Matrix4x4 localRotate = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90f));

            GUI.matrix = savedMatrix * globalRotateAndTranslate * move * localRotate;

            float alpha = Mathf.Sin(progress * Mathf.PI) * 0.7f;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            Rect drawRect = new Rect(-drawW / 2f, -drawH / 2f, drawW, drawH);
            GUI.DrawTexture(drawRect, tex, ScaleMode.StretchToFill);

            GUI.color = savedColor;
            GUI.matrix = savedMatrix;
        }
    }
}