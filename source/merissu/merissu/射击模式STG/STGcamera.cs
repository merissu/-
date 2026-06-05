using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{

    public class STGCamera : CameraMapConfig_Normal
    {
        private static readonly float[] ZoomLevels =
        {
            6f,
            12f,
            18f,
            24f,
            30f
        };

        private static int currentZoomIndex = -1;
        private static int lastZoomFrame = -1; 

        public STGCamera()
        {
            dollyRateKeys = 0f;
            dollyRateScreenEdge = 0f;

            sizeRange = new FloatRange(1.5f, 60f);

            zoomSpeed = 5f;
        }

        public static void CycleZoom()
        {
            if (Find.CameraDriver == null)
                return;

            if (Time.frameCount == lastZoomFrame)
                return;
            lastZoomFrame = Time.frameCount;

            float currentSize = Find.CameraDriver.rootSize;

            int closestIndex = 0;
            float minDiff = float.MaxValue;
            for (int i = 0; i < ZoomLevels.Length; i++)
            {
                float diff = Mathf.Abs(ZoomLevels[i] - currentSize);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestIndex = i;
                }
            }

            currentZoomIndex = (closestIndex + 1) % ZoomLevels.Length;

            Find.CameraDriver.SetRootSize(ZoomLevels[currentZoomIndex]);
        }

        public static float CurrentZoomSize =>
            currentZoomIndex >= 0 ? ZoomLevels[currentZoomIndex] : Find.CameraDriver.rootSize;
    }
    public static class SimpleCameraBridge
    {
        private static MethodInfo _configPatchMethod;
        private static bool _initialized = false;

        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Log.Message("已兼容简易相机...");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.Name == "CameraConfigPatch" && type.Namespace != null && type.Namespace.Contains("SimpleCamera"))
                    {
                        _configPatchMethod = type.GetMethod("ConfigPatch", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                        if (_configPatchMethod != null)
                        {
                            Log.Message("成功兼容SimpleCameraSetting");
                            return;
                        }
                    }
                }
            }

            Log.Warning("STG模式运行成功");
        }

        public static void ResetSimpleCamera()
        {
            Init();

            if (_configPatchMethod != null)
            {
                try
                {
                    _configPatchMethod.Invoke(null, null);
                    Log.Message("简易相机配置刷新");
                }
                catch (Exception e)
                {
                    Log.Error($"[STG]调用简易相机刷新失败: {e.InnerException?.Message ?? e.Message}");
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    public static class STGModAutoInitializer
    {
        static STGModAutoInitializer()
        {
            Log.Message("[STG] 正在兼容简易相机");

            var harmony = new Harmony("merissu.stgmod.autofixer");

            var original = AccessTools.Method(typeof(Map), nameof(Map.ConstructComponents));
            var postfix = AccessTools.Method(typeof(STGModAutoInitializer), nameof(MapComponentsPostfix));

            try
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                Log.Message("[STG] 兼容简易相机成功！");
            }
            catch (Exception e)
            {
                Log.Error($"[STG] 兼容简易相机失败: {e.Message}");
            }
        }

        private static void MapComponentsPostfix(Map __instance)
        {
            if (Find.CameraDriver != null)
            {
                TriggerFix();
            }
            else
            {
                LongEventHandler.ExecuteWhenFinished(TriggerFix);
            }
        }

        private static void TriggerFix()
        {
            if (Find.CameraDriver == null) return;

            if (Find.CameraDriver.config is STGCamera) return;

            Log.Message("[STG] 正在兼容简易相机...");
            SimpleCameraBridge.ResetSimpleCamera();
        }
    }
}