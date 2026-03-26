using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class WeatherVisualSettingsExtension : DefModExtension
    {
        public bool hasTyndallEffect = false;       
        public string rainbowTexturePath;           
        public Color tint = Color.white;            
    }

    public class SunBeamInstance
    {
        public Texture2D tex;
        public Material mat;
        public Vector2 pos;
        public float speed;
        public float alpha = 0f;
        public float lifeTime = 0f;
        public float maxLifeTime;
        public float width, height;
        private float shearOffset;
        private static readonly Rect SafeSourceRect = new Rect(0.02f, 0.02f, 0.96f, 0.96f);
        public bool IsDead => lifeTime >= maxLifeTime;

        public SunBeamInstance(Texture2D texture, Material material)
        {
            tex = texture; mat = material;
            pos = new Vector2(Rand.Range(-100f, UI.screenWidth + 200f), -20f);
            speed = Rand.Range(10f, 30f) * (Rand.Value > 0.5f ? 1f : -1f);
            maxLifeTime = Rand.Range(6f, 12f);
            height = UI.screenHeight * Rand.Range(0.6f, 0.8f);
            width = height * ((float)tex.width / tex.height);
            shearOffset = Rand.Range(150f, 250f);
        }

        public void Update(float dt)
        {
            lifeTime += dt;
            pos.x += speed * dt;
            if (lifeTime < 2f) alpha = lifeTime / 2f;
            else if (lifeTime > maxLifeTime - 2f) alpha = (maxLifeTime - lifeTime) / 2f;
            else alpha = 1f;
        }

        public void Draw()
        {
            Matrix4x4 savedMatrix = GUI.matrix;
            Color savedColor = GUI.color;
            float shearX = shearOffset / height;
            Matrix4x4 shearMatrix = Matrix4x4.identity;
            shearMatrix[0, 1] = -shearX;
            Matrix4x4 transformation = Matrix4x4.Translate(new Vector3(pos.x, pos.y, 0)) * shearMatrix * Matrix4x4.Translate(new Vector3(-pos.x, -pos.y, 0));
            GUI.matrix = savedMatrix * transformation;
            GUI.color = new Color(1f, 1f, 1f, alpha * 0.4f); 
            Rect drawRect = new Rect(pos.x, pos.y, width, height);
            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(drawRect, tex, SafeSourceRect, 0, 0, 0, 0, GUI.color, mat);
            }
            GUI.color = savedColor;
            GUI.matrix = savedMatrix;
        }
    }

    public class Merissu_WeatherEffectManager : GameComponent
    {
        private string lastWeatherDef = "";
        private List<SunBeamInstance> beams = new List<SunBeamInstance>();
        private string[] beamPaths = { "weather/weatherA001", "weather/weatherA002", "weather/weatherA003", "weather/weatherA004" };
        private Dictionary<string, Material> beamMaterials = new Dictionary<string, Material>();

        private Texture2D rainbowTex;
        private Material rainbowMat;
        private WeatherVisualSettingsExtension currentVisuals;

        public Merissu_WeatherEffectManager(Game game) : base() { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 30 != 0) return;

            Map map = Find.CurrentMap;
            WeatherDef curWeather = map?.weatherManager?.curWeather;
            if (curWeather?.defName != lastWeatherDef)
            {
                lastWeatherDef = curWeather?.defName ?? "";
                currentVisuals = curWeather?.GetModExtension<WeatherVisualSettingsExtension>();

                if (currentVisuals != null && !currentVisuals.rainbowTexturePath.NullOrEmpty())
                {
                    rainbowTex = ContentFinder<Texture2D>.Get(currentVisuals.rainbowTexturePath);
                    if (rainbowTex != null)
                        rainbowMat = MaterialPool.MatFrom(rainbowTex, ShaderDatabase.MoteGlow, Color.white);
                }
                else
                {
                    rainbowTex = null;
                }
            }
        }

        public override void GameComponentOnGUI()
        {
            if (currentVisuals == null || Find.CurrentMap == null) return;

            float glow = Find.CurrentMap.skyManager.CurSkyGlow;
            if (glow < 0.3f) return; 

            if (rainbowTex != null) DrawFullColorRainbow(glow);

            if (currentVisuals.hasTyndallEffect) DrawTyndallBeams(glow);
        }

        private void DrawFullColorRainbow(float glow)
        {
            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;
            float drawW = screenW;
            float drawH = screenW * 0.5f; 

            if (drawH < screenH) 
            {
                drawH = screenH;
                drawW = screenH * 2f;
            }

            Rect rect = new Rect((screenW - drawW) / 2f, (screenH - drawH) / 2f, drawW, drawH);

            float alpha = Mathf.Min(currentVisuals.tint.a, (glow - 0.3f) * 1.5f);
            GUI.color = new Color(currentVisuals.tint.r, currentVisuals.tint.g, currentVisuals.tint.b, alpha);

            GenUI.DrawTextureWithMaterial(rect, rainbowTex, rainbowMat);
            GUI.color = Color.white;
        }

        private void DrawTyndallBeams(float glow)
        {
            if (glow < 0.5f) { beams.Clear(); return; }

            while (beams.Count < 4)
            {
                string path = beamPaths.RandomElement();
                if (!beamMaterials.ContainsKey(path))
                    beamMaterials[path] = MaterialPool.MatFrom(ContentFinder<Texture2D>.Get(path), ShaderDatabase.MoteGlow, Color.white);
                beams.Add(new SunBeamInstance(ContentFinder<Texture2D>.Get(path), beamMaterials[path]));
            }

            for (int i = beams.Count - 1; i >= 0; i--)
            {
                beams[i].Update(Time.deltaTime);
                if (beams[i].IsDead) beams.RemoveAt(i);
                else beams[i].Draw();
            }
        }
    }
}