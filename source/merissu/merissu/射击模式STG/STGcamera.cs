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
}