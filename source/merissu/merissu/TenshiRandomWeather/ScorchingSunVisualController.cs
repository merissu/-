using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class ScorchingSunVisualController : GameComponent
    {
        private const string TargetWeatherDef = "scorchingsun";
        private const string SunTexturePath = "weather/weatherP000";

        private List<SunBeamInstance> beams = new List<SunBeamInstance>();
        private string[] beamPaths = { "weather/weatherA001", "weather/weatherA002", "weather/weatherA003", "weather/weatherA004" };
        private Dictionary<string, Material> beamMaterials = new Dictionary<string, Material>();

        private Texture2D sunTex;
        private Material sunMat;

        public ScorchingSunVisualController(Game game) : base() { }

        public override void GameComponentOnGUI()
        {
            Map map = Find.CurrentMap;
            if (map == null || map.weatherManager?.curWeather?.defName != TargetWeatherDef)
            {
                beams.Clear();
                return;
            }

            DrawMegaSun();

            if (map.skyManager.CurSkyGlow > 0.3f)
            {
                DrawSunBeams();
            }
        }

        private void DrawMegaSun()
        {
            if (sunTex == null)
            {
                sunTex = ContentFinder<Texture2D>.Get(SunTexturePath, false);
                if (sunTex != null)
                    sunMat = MaterialPool.MatFrom(sunTex, ShaderDatabase.MoteGlow, Color.white);
            }

            if (sunTex != null && sunMat != null)
            {
                float sunSize = UI.screenWidth * 1.2f;

                float xPos = (UI.screenWidth - sunSize) / 2f;

                float yPos = -sunSize * 0.6f;

                Rect sunRect = new Rect(xPos, yPos, sunSize, sunSize);

                float pulsate = 1f + Mathf.Sin(Time.realtimeSinceStartup * 0.8f) * 0.04f;
                Vector2 center = sunRect.center;
                sunRect.width *= pulsate;
                sunRect.height *= pulsate;
                sunRect.center = center;

                GUI.color = new Color(1f, 0.92f, 0.85f, 0.85f);

                GenUI.DrawTextureWithMaterial(sunRect, sunTex, sunMat);
                GUI.color = Color.white;
            }
        }
        private void DrawSunBeams()
        {
            while (beams.Count < 5) 
            {
                string path = beamPaths.RandomElement();
                if (!beamMaterials.ContainsKey(path))
                {
                    Texture2D tex = ContentFinder<Texture2D>.Get(path, false);
                    if (tex != null)
                        beamMaterials[path] = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, Color.white);
                }

                if (beamMaterials.ContainsKey(path))
                    beams.Add(new SunBeamInstance(ContentFinder<Texture2D>.Get(path), beamMaterials[path]));
            }

            for (int i = beams.Count - 1; i >= 0; i--)
            {
                beams[i].Update(Time.deltaTime);
                if (beams[i].IsDead) beams.RemoveAt(i);
                else beams[i].Draw();
            }
        }

        private class SunBeamInstance
        {
            public Texture2D tex;
            public Material mat;
            public Vector2 pos;
            public float speed;
            public float alpha;
            public float lifeTime;
            public float maxLifeTime;
            public float width, height;
            private float shearOffset;
            private static readonly Rect SafeSourceRect = new Rect(0.05f, 0.05f, 0.9f, 0.9f);

            public bool IsDead => lifeTime >= maxLifeTime;

            public SunBeamInstance(Texture2D texture, Material material)
            {
                tex = texture;
                mat = material;
                pos = new Vector2(Rand.Range(-300f, UI.screenWidth + 300f), -100f);
                speed = Rand.Range(20f, 50f) * (Rand.Value > 0.5f ? 1f : -1f);
                maxLifeTime = Rand.Range(6f, 12f);
                height = UI.screenHeight * 0.9f;
                width = height * ((float)tex.width / tex.height);
                shearOffset = Rand.Range(200f, 400f);
                lifeTime = 0f;
            }

            public void Update(float dt)
            {
                lifeTime += dt;
                pos.x += speed * dt;
                if (lifeTime < 1.5f) alpha = lifeTime / 1.5f;
                else if (lifeTime > maxLifeTime - 1.5f) alpha = (maxLifeTime - lifeTime) / 1.5f;
                else alpha = 1f;
            }

            public void Draw()
            {
                Matrix4x4 savedMatrix = GUI.matrix;
                float shearX = shearOffset / height;
                Matrix4x4 shearMatrix = Matrix4x4.identity;
                shearMatrix[0, 1] = -shearX;

                Matrix4x4 trans = Matrix4x4.Translate(new Vector3(pos.x, pos.y, 0)) * shearMatrix * Matrix4x4.Translate(new Vector3(-pos.x, -pos.y, 0));

                GUI.matrix = savedMatrix * trans;
                GUI.color = new Color(1f, 0.85f, 0.6f, alpha * 0.4f);

                Rect drawRect = new Rect(pos.x, pos.y, width, height);
                Graphics.DrawTexture(drawRect, tex, SafeSourceRect, 0, 0, 0, 0, GUI.color, mat);

                GUI.color = Color.white;
                GUI.matrix = savedMatrix;
            }
        }
    }
}