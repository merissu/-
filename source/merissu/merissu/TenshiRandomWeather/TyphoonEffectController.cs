using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class TyphoonEffectController : GameComponent
    {
        private const string TargetWeatherDef = "typhoon";

        private const float CYCLE_DURATION = 2.0f;
        private const float VISIBLE_FRACTION = 0.4f;

        private List<Texture2D> textures = new List<Texture2D>();
        private bool initialized = false;

        public TyphoonEffectController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            if (Find.CurrentMap?.weatherManager?.curWeather?.defName != TargetWeatherDef) return;
            if (!initialized) InitResources();

            DrawViolentStormTextures();
        }

        private void InitResources()
        {
            string[] paths = { "weather/weatherD000", "weather/weatherB000" };
            foreach (var path in paths)
            {
                Texture2D tex = ContentFinder<Texture2D>.Get(path, false);
                if (tex != null) textures.Add(tex);
            }
            initialized = true;
        }

        private void DrawViolentStormTextures()
        {
            if (textures.Count == 0) return;

            float time = RealTime.LastRealTime;

            for (int i = 0; i < textures.Count; i++)
            {
                float phase = (time + (i * (CYCLE_DURATION / (float)textures.Count))) % CYCLE_DURATION;

                if (phase < CYCLE_DURATION * VISIBLE_FRACTION)
                {
                    float progress = phase / (CYCLE_DURATION * VISIBLE_FRACTION);

                    float jitterX = Mathf.Sin(time * 60f + i) * 8f;
                    float jitterY = Mathf.Cos(time * 60f + i) * 8f;

                    DrawSingleViolentLayer(textures[i], progress, new Vector2(jitterX, jitterY));
                }
            }
        }

        private void DrawSingleViolentLayer(Texture2D tex, float progress, Vector2 jitter)
        {
            float screenDiagonal = Mathf.Sqrt(Mathf.Pow(UI.screenWidth, 2) + Mathf.Pow(UI.screenHeight, 2));
            float drawW = screenDiagonal * 2.2f;
            float originalAspect = (float)tex.height / tex.width;
            float drawH = drawW / originalAspect;

            Matrix4x4 savedMatrix = GUI.matrix;
            Color savedColor = GUI.color;

            Vector2 center = new Vector2(UI.screenWidth / 2f + jitter.x, UI.screenHeight / 2f + jitter.y);
            float pathAngle = Mathf.Atan2(UI.screenHeight, UI.screenWidth) * Mathf.Rad2Deg;

            float easedProgress = progress * progress;
            float curX = Mathf.Lerp(drawW, -drawW, easedProgress);

            Matrix4x4 globalRotateAndTranslate = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, -pathAngle), Vector3.one);
            Matrix4x4 move = Matrix4x4.Translate(new Vector3(curX, 0, 0));
            Matrix4x4 localRotate = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90f));

            GUI.matrix = savedMatrix * globalRotateAndTranslate * move * localRotate;

            float alpha = Mathf.Sin(progress * Mathf.PI) * 0.9f;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            Rect drawRect = new Rect(-drawW / 2f, -drawH / 2f, drawW, drawH);
            GUI.DrawTexture(drawRect, tex, ScaleMode.StretchToFill);

            GUI.color = savedColor;
            GUI.matrix = savedMatrix;
        }
    }
}