using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class WeatherBackdropSettings : ModSettings
    {
        public bool debugMode = false;

        public float sideUVWidth = 0.289f;
        public float topBottomUVHeight = 0.270f;
        public float viewAspect = 0.807f;

        public float horRenderWidth = 170f;
        public float verRenderHeight = 100f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref debugMode, "debugMode", false);
            Scribe_Values.Look(ref sideUVWidth, "sideUVWidth", 0.289f);
            Scribe_Values.Look(ref topBottomUVHeight, "topBottomUVHeight", 0.270f);
            Scribe_Values.Look(ref viewAspect, "viewAspect", 0.807f);
            Scribe_Values.Look(ref horRenderWidth, "horRenderWidth", 170f);
            Scribe_Values.Look(ref verRenderHeight, "verRenderHeight", 100f);
        }
    }

    public class WeatherBackdropMod : Mod
    {
        public static WeatherBackdropSettings settings;
        public WeatherBackdropMod(ModContentPack content) : base(content) { settings = GetSettings<WeatherBackdropSettings>(); }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);
            l.CheckboxLabeled("开启实时调试模式 (Enable Debug Mode)", ref settings.debugMode);
            l.GapLine();

            l.Label("【快速预设配置】");

            float btnWidth = (inRect.width - 25f) / 2f;

            Rect row1 = l.GetRect(30f);
            if (Widgets.ButtonText(new Rect(row1.x, row1.y, btnWidth, 30f), "适配 325x325 (绝)"))
            {
                ApplyPreset(170f, 100f, 0.257f, 0.270f, 0.807f);
                Messages.Message("已应用 325x325 极限比例预设", MessageTypeDefOf.PositiveEvent, false);
            }
            if (Widgets.ButtonText(new Rect(row1.x + btnWidth + 10f, row1.y, btnWidth, 30f), "适配 300x300 (极)"))
            {
                ApplyPreset(170f, 100f, 0.268f, 0.270f, 0.807f);
                Messages.Message("已应用 300x300 极致比例预设", MessageTypeDefOf.PositiveEvent, false);
            }

            l.Gap(5f);

            Rect row2 = l.GetRect(30f);
            if (Widgets.ButtonText(new Rect(row2.x, row2.y, btnWidth, 30f), "适配 275x275 (大)"))
            {
                ApplyPreset(170f, 100f, 0.277f, 0.270f, 0.807f);
                Messages.Message("已应用 275x275 黄金比例预设", MessageTypeDefOf.PositiveEvent, false);
            }
            if (Widgets.ButtonText(new Rect(row2.x + btnWidth + 10f, row2.y, btnWidth, 30f), "适配 250x250 (标)"))
            {
                ApplyPreset(170f, 100f, 0.289f, 0.270f, 0.807f);
                Messages.Message("已应用 250x250 黄金比例预设", MessageTypeDefOf.PositiveEvent, false);
            }

            l.Gap(5f);

            Rect row3 = l.GetRect(30f);
            if (Widgets.ButtonText(new Rect(row3.x, row3.y, btnWidth, 30f), "适配 225x225 (中)"))
            {
                ApplyPreset(170f, 100f, 0.299f, 0.270f, 0.807f);
                Messages.Message("已应用 225x225 推荐比例预设", MessageTypeDefOf.PositiveEvent, false);
            }
            if (Widgets.ButtonText(new Rect(row3.x + btnWidth + 10f, row3.y, btnWidth, 30f), "适配 200x200 (小)"))
            {
                ApplyPreset(170f, 100f, 0.313f, 0.270f, 0.807f);
                Messages.Message("已应用 200x200 推荐比例预设", MessageTypeDefOf.PositiveEvent, false);
            }

            l.Gap(20f);

            if (settings.debugMode)
            {
                GUI.color = Color.green;
                l.Label("【纵向控制 - 上下幕布 (North & South)】");
                GUI.color = Color.white;
                l.Label($"渲染物理高度 (Render Height): {settings.verRenderHeight:F0}");
                settings.verRenderHeight = l.Slider(settings.verRenderHeight, 10f, 2000f);
                l.Label($"原图采样高度比例 (UV Height 0-1): {settings.topBottomUVHeight:F3}");
                settings.topBottomUVHeight = l.Slider(settings.topBottomUVHeight, 0f, 1f);
                l.Gap();

                GUI.color = Color.cyan;
                l.Label("【横向控制 - 左右幕布 (West & East)】");
                GUI.color = Color.white;
                l.Label($"渲染物理宽度 (Render Width): {settings.horRenderWidth:F0}");
                settings.horRenderWidth = l.Slider(settings.horRenderWidth, 10f, 2000f);
                l.Label($"原图采样宽度比例,一般只用调整这个 (UV Width 0-1): {settings.sideUVWidth:F3}");
                settings.sideUVWidth = l.Slider(settings.sideUVWidth, 0f, 1f);
                l.Gap();

                GUI.color = Color.yellow;
                l.Label("【全局比例修正】");
                GUI.color = Color.white;
                l.Label($"镜头高宽比 (ViewAspect): {settings.viewAspect:F3}");
                settings.viewAspect = l.Slider(settings.viewAspect, 0.1f, 2.0f);

                Patch_MapEdgeClipDrawer_DrawClippers.ClearCache();

                l.Gap(20f);
                Text.Font = GameFont.Tiny;
                l.Label("提示：使用上方按钮可快速切换不同地图尺寸的推荐比例。");
                Text.Font = GameFont.Small;
            }
            l.End();
        }      
        private void ApplyPreset(float hW, float vH, float sUV, float tbUV, float aspect)
        {
            settings.horRenderWidth = hW;
            settings.verRenderHeight = vH;
            settings.sideUVWidth = sUV;
            settings.topBottomUVHeight = tbUV;
            settings.viewAspect = aspect;
            Patch_MapEdgeClipDrawer_DrawClippers.ClearCache();
        }

        public override string SettingsCategory() => "绯想之剑调试面板";
    }

    public class WeatherBackdropExtension : DefModExtension
    {
        public string texturePath;
        public float alpha = 1f;
        public Color tint = Color.white;
        public bool splitOneTextureToFour = true;
        public int sourceWidth = 0, sourceHeight = 0;
        public int trimBottomPixels = 0, trimTopPixels = 0, trimLeftPixels = 0, trimRightPixels = 0;
        public bool cropToViewAspect = true;
        public float viewAspectHOverW = 0.807f;
        public bool keepBottomWhenCropHeight = true;
        public float sideWidthPercent = 0.289f;
        public float topBottomHeightPercent = 0.270f;
    }

    [StaticConstructorOnStartup]
    public static class WeatherBackdropBootstrap
    {
        static WeatherBackdropBootstrap() { new Harmony("merissu.weatherbackdrop.normalclipper").PatchAll(); }
    }

    internal class BackdropMatSet { public Material west, east, north, south; }

    [HarmonyPatch(typeof(MapEdgeClipDrawer), nameof(MapEdgeClipDrawer.DrawClippers))]
    public static class Patch_MapEdgeClipDrawer_DrawClippers
    {
        private static readonly Dictionary<WeatherBackdropExtension, BackdropMatSet> Cache = new Dictionary<WeatherBackdropExtension, BackdropMatSet>();
        public static void ClearCache() => Cache.Clear();

        public static bool Prefix(Map map)
        {
            if (map == null || !map.DrawMapClippers || map.weatherManager == null) return false;
            var ext = map.weatherManager.CurWeatherPerceived?.GetModExtension<WeatherBackdropExtension>();
            if (ext == null || ext.texturePath.NullOrEmpty()) return true;

            BackdropMatSet mats = GetOrCreate(ext);
            if (mats != null) DrawCustomClippers(map, mats);
            return false;
        }

        private static BackdropMatSet GetOrCreate(WeatherBackdropExtension ext)
        {
            if (!WeatherBackdropMod.settings.debugMode && Cache.TryGetValue(ext, out var cached)) return cached;

            Texture2D tex = ContentFinder<Texture2D>.Get(ext.texturePath, false);
            if (tex == null) return null;

            float sideUV = WeatherBackdropMod.settings.debugMode ? WeatherBackdropMod.settings.sideUVWidth : ext.sideWidthPercent;
            float tbUV = WeatherBackdropMod.settings.debugMode ? WeatherBackdropMod.settings.topBottomUVHeight : ext.topBottomHeightPercent;
            float aspect = WeatherBackdropMod.settings.debugMode ? WeatherBackdropMod.settings.viewAspect : ext.viewAspectHOverW;

            Rect uv = BuildTrimmedUV(ext, (ext.sourceWidth > 0 ? ext.sourceWidth : tex.width), (ext.sourceHeight > 0 ? ext.sourceHeight : tex.height));
            if (ext.cropToViewAspect) uv = CropUVToAspect(uv, aspect, ext.keepBottomWhenCropHeight);

            float midH = Mathf.Max(0f, 1f - (tbUV * 2f));
            Color c = ext.tint; c.a *= Mathf.Clamp01(ext.alpha);

            var set = new BackdropMatSet();
            set.north = MakeMat(tex, c, SubRect(uv, 0f, 1f - tbUV, 1f, tbUV));
            set.south = MakeMat(tex, c, SubRect(uv, 0f, 0f, 1f, tbUV));
            set.west = MakeMat(tex, c, SubRect(uv, 0f, tbUV, sideUV, midH));
            set.east = MakeMat(tex, c, SubRect(uv, 1f - sideUV, tbUV, sideUV, midH));

            if (!WeatherBackdropMod.settings.debugMode) Cache[ext] = set;
            return set;
        }

        private static void DrawCustomClippers(Map map, BackdropMatSet mats)
        {
            IntVec3 size = map.Size;
            float y = AltitudeLayer.WorldClipper.AltitudeFor();

            float hW = WeatherBackdropMod.settings.horRenderWidth;
            float vH = WeatherBackdropMod.settings.verRenderHeight;

            Vector3 verScale = new Vector3(size.x + (hW * 2f), 1f, vH);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(new Vector3(size.x / 2f, y, size.z + vH / 2f), Quaternion.identity, verScale), mats.north, 0);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(new Vector3(size.x / 2f, y, -vH / 2f), Quaternion.identity, verScale), mats.south, 0);

            Vector3 horScale = new Vector3(hW, 1f, size.z + 0.5f);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(new Vector3(-hW / 2f, y, size.z / 2f), Quaternion.identity, horScale), mats.west, 0);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(new Vector3(size.x + hW / 2f, y, size.z / 2f), Quaternion.identity, horScale), mats.east, 0);
        }

        private static Material MakeMat(Texture2D tex, Color color, Rect uv)
        {
            Material m = new Material(ShaderDatabase.MetaOverlay) { mainTexture = tex, color = color };
            m.mainTextureScale = new Vector2(uv.width, uv.height);
            m.mainTextureOffset = new Vector2(uv.x, uv.y);
            return m;
        }

        private static Rect BuildTrimmedUV(WeatherBackdropExtension ext, int srcW, int srcH)
        {
            float uMin = Mathf.Clamp01((float)ext.trimLeftPixels / srcW);
            float uMax = 1f - Mathf.Clamp01((float)ext.trimRightPixels / srcW);
            float vMin = Mathf.Clamp01((float)ext.trimBottomPixels / srcH);
            float vMax = 1f - Mathf.Clamp01((float)ext.trimTopPixels / srcH);
            return new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
        }

        private static Rect CropUVToAspect(Rect uv, float targetHOverW, bool keepBottom)
        {
            float w = uv.width, h = uv.height, cur = h / w;
            if (cur < targetHOverW)
            {
                float newW = h / targetHOverW;
                return new Rect(uv.x + (w - newW) * 0.5f, uv.y, newW, h);
            }
            if (cur > targetHOverW)
            {
                float newH = w * targetHOverW;
                return new Rect(uv.x, uv.y + (keepBottom ? 0 : (h - newH) * 0.5f), w, newH);
            }
            return uv;
        }

        private static Rect SubRect(Rect p, float x, float y, float w, float h) => new Rect(p.x + p.width * x, p.y + p.height * y, p.width * w, p.height * h);
    }
}