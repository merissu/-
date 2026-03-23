using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace merissu
{
    public class WeatherMusicExtension : DefModExtension
    {
        public string clipPath;
        public float volume = 1f;
        public float fadeOutSeconds = 1.5f;
    }

    public class WeatherEffectController : GameComponent
    {
        private const string TargetWeatherDefName = "HighClearSky";
        private const float TIME_FADE_IN = 0.6f;
        private const float TIME_STAY = 3.0f;
        private const float TIME_FADE_OUT = 0.8f;
        private float TotalDuration => TIME_FADE_IN + TIME_STAY + TIME_FADE_OUT;

        private string lastWeatherDef = "";
        private bool isEffectActive = false;
        private float effectTimer = 0f;
        private Texture2D weatherTex;

        private AudioSource weatherMusicSource;
        private WeatherMusicExtension currentMusicExt;
        private bool isMusicFadingOut = false;
        private float musicFadeOutTimer = 0f;
        private float musicFadeStartVolume = 1f;

        private Texture2D dayOverlayTex;
        private Material dayOverlayMat;
        private List<SunBeamInstance> beams = new List<SunBeamInstance>();
        private string[] beamPaths = { "weather/weatherA001", "weather/weatherA002", "weather/weatherA003", "weather/weatherA004" };
        private Dictionary<string, Material> beamMaterials = new Dictionary<string, Material>();

        public WeatherEffectController(Game game) : base() { }

        public override void FinalizeInit() => EnsureAudioSource();
        public override void LoadedGame() => EnsureAudioSource();
        public override void StartedNewGame() => EnsureAudioSource();
        private void CleanupAudio()
        {
            if (weatherMusicSource != null)
            {
                weatherMusicSource.Stop();
                Object.Destroy(weatherMusicSource.gameObject);
                weatherMusicSource = null;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                CleanupAudio();
            }
        }

        private void EnsureAudioSource()
        {
            if (weatherMusicSource != null) return;

            GameObject go = new GameObject("Merissu_WeatherMusicSource");

            weatherMusicSource = go.AddComponent<AudioSource>();
            weatherMusicSource.playOnAwake = false;
            weatherMusicSource.loop = true; 
            weatherMusicSource.spatialBlend = 0f;
            weatherMusicSource.priority = 0;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 20 != 0) return;

            Map map = Find.CurrentMap;
            string currentWeather = map?.weatherManager?.curWeather?.defName ?? "";

            if (currentWeather == TargetWeatherDefName && lastWeatherDef != TargetWeatherDefName)
            {
                StartWeatherAnimation();
                StartWeatherMusic(map);
            }
            else if (currentWeather != TargetWeatherDefName && lastWeatherDef == TargetWeatherDefName)
            {
                StopWeatherMusic(false);
            }

            lastWeatherDef = currentWeather;
        }

        public override void GameComponentUpdate()
        {
            HandleMusicVolumeAndFade();
        }

        private void StartWeatherAnimation()
        {
            isEffectActive = true;
            effectTimer = 0f;
            weatherTex = ContentFinder<Texture2D>.Get("weather/weatherNameB000");
        }

        private void StartWeatherMusic(Map map)
        {
            WeatherDef weather = map?.weatherManager?.curWeather;
            WeatherMusicExtension ext = weather?.GetModExtension<WeatherMusicExtension>();

            if (ext == null || string.IsNullOrEmpty(ext.clipPath)) return;

            AudioClip clip = ContentFinder<AudioClip>.Get(ext.clipPath, false);
            if (clip == null) return;

            Find.MusicManagerPlay?.ForceFadeoutAndSilenceFor(600f, 1.0f, true);

            currentMusicExt = ext;
            isMusicFadingOut = false;

            weatherMusicSource.clip = clip;
            weatherMusicSource.volume = ext.volume * Prefs.VolumeMusic;
            weatherMusicSource.Play();
        }

        private void StopWeatherMusic(bool immediate)
        {
            if (weatherMusicSource == null || !weatherMusicSource.isPlaying) return;

            if (immediate)
            {
                weatherMusicSource.Stop();
                isMusicFadingOut = false;
                if (Find.MusicManagerPlay != null) Find.MusicManagerPlay.ScheduleNewSong();
            }
            else
            {
                isMusicFadingOut = true;
                musicFadeOutTimer = 0f;
                musicFadeStartVolume = weatherMusicSource.volume;
            }
        }

        private void HandleMusicVolumeAndFade()
        {
            if (weatherMusicSource == null || !weatherMusicSource.isPlaying) return;

            if (isMusicFadingOut)
            {
                musicFadeOutTimer += Time.unscaledDeltaTime;
                float duration = currentMusicExt?.fadeOutSeconds ?? 1.5f;
                float p = Mathf.Clamp01(musicFadeOutTimer / duration);

                weatherMusicSource.volume = Mathf.Lerp(musicFadeStartVolume, 0f, p);

                if (p >= 1f)
                {
                    weatherMusicSource.Stop();
                    isMusicFadingOut = false;
                    if (Find.MusicManagerPlay != null) Find.MusicManagerPlay.ScheduleNewSong();
                }
            }
            else if (currentMusicExt != null)
            {
                weatherMusicSource.volume = currentMusicExt.volume * Prefs.VolumeMusic;
            }
        }


        public override void GameComponentOnGUI()
        {
            DrawDayOverlay();
            DrawSunBeams();
            DrawSwitchAnimation();
        }

        private void DrawDayOverlay()
        {
            Map map = Find.CurrentMap;
            if (map == null || map.weatherManager.curWeather.defName != TargetWeatherDefName) return;

            float glowAlpha = map.skyManager.CurSkyGlow;
            if (glowAlpha <= 0.02f) return;

            if (dayOverlayTex == null)
            {
                dayOverlayTex = ContentFinder<Texture2D>.Get("weather/weatherA000");
                if (dayOverlayTex != null)
                    dayOverlayMat = MaterialPool.MatFrom(dayOverlayTex, ShaderDatabase.MoteGlow, Color.white);
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

        private void DrawSunBeams()
        {
            Map map = Find.CurrentMap;
            if (map == null || map.weatherManager.curWeather.defName != TargetWeatherDefName || map.skyManager.CurSkyGlow < 0.5f)
            {
                beams.Clear();
                return;
            }

            while (beams.Count < 4)
            {
                string path = beamPaths.RandomElement();
                Texture2D tex = ContentFinder<Texture2D>.Get(path);
                if (!beamMaterials.ContainsKey(path))
                    beamMaterials[path] = MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, Color.white);

                beams.Add(new SunBeamInstance(tex, beamMaterials[path]));
            }

            for (int i = beams.Count - 1; i >= 0; i--)
            {
                beams[i].Update(Time.deltaTime);
                if (beams[i].IsDead) beams.RemoveAt(i);
                else beams[i].Draw();
            }
        }

        private void DrawSwitchAnimation()
        {
            if (!isEffectActive || weatherTex == null) return;
            effectTimer += Time.deltaTime;
            if (effectTimer > TotalDuration) { isEffectActive = false; return; }

            float drawW = 512f, drawH = 256f, alpha = 1f;

            if (effectTimer < TIME_FADE_IN)
            {
                float p = effectTimer / TIME_FADE_IN; alpha = p;
                float s = Mathf.Lerp(1.4f, 1.0f, p); drawW *= s; drawH *= s;
            }
            else if (effectTimer < TIME_FADE_IN + TIME_STAY) { alpha = 1f; }
            else
            {
                float op = (effectTimer - TIME_FADE_IN - TIME_STAY) / TIME_FADE_OUT;
                alpha = 1f - op; drawW *= (1f + op * 1.2f); drawH *= (1f - op * 0.85f);
            }

            Rect rect = new Rect((UI.screenWidth - drawW) / 2f, (UI.screenHeight * 0.25f) - (drawH / 2f), drawW, drawH);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, weatherTex, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        private class SunBeamInstance
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

                GUI.color = new Color(1f, 1f, 1f, alpha * 0.5f);

                Rect drawRect = new Rect(pos.x, pos.y, width, height);

                if (Event.current.type == EventType.Repaint)
                {
                    Graphics.DrawTexture(drawRect, tex, SafeSourceRect, 0, 0, 0, 0, GUI.color, mat);
                }

                GUI.color = savedColor;
                GUI.matrix = savedMatrix;
            }
        }
    }
}