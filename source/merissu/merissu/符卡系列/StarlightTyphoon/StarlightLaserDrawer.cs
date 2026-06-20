using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class StarlightLaserDrawer
    {
        // 像素定义
        private const float TexWidth = 128f;   // 贴图宽度（激光粗细）
        private const float TexHeight = 217f;  // 贴图总长度

        // 各部分像素高度
        private const float HeadPx = 100f;     // 发射点部分
        private const float BodyPx = 40f;      // 中间可拉伸部分
        private const float TailPx = 77f;      // 击中部分

        private const float HeadUVLen = HeadPx / TexHeight;
        private const float BodyUVLen = BodyPx / TexHeight;
        private const float TailUVLen = TailPx / TexHeight;

        private const float HeadUVOffset = 0f;
        private const float BodyUVOffset = HeadUVLen;
        private const float TailUVOffset = HeadUVLen + BodyUVLen;

        private static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        public static void DrawLaser(
            Vector3 start,
            Vector3 end,
            Material mat,
            float thickness = 2f) // 粗细
        {
            if (mat == null) return;

            Vector3 dir = end - start;
            float totalLength = dir.magnitude;

            if (totalLength < 0.1f) return;

            Quaternion rot = Quaternion.LookRotation(dir); 

            float pixelScale = thickness / TexWidth;

            float headWorldLen = HeadPx * pixelScale;
            float tailWorldLen = TailPx * pixelScale;

            if (totalLength < (headWorldLen + tailWorldLen))
            {
                float scale = totalLength / (headWorldLen + tailWorldLen);
                headWorldLen *= scale;
                tailWorldLen *= scale;
            }

            float bodyWorldLen = totalLength - headWorldLen - tailWorldLen;

            Mesh mesh = MeshPool.plane10;

            Vector3 headPos = start + (dir.normalized * (headWorldLen * 0.5f));
            DrawSegment(mesh, mat, headPos, rot, thickness, headWorldLen, HeadUVLen, HeadUVOffset);

            Vector3 tailPos = end - (dir.normalized * (tailWorldLen * 0.5f));
            DrawSegment(mesh, mat, tailPos, rot, thickness, tailWorldLen, TailUVLen, TailUVOffset);

            if (bodyWorldLen > 0.001f)
            {
                Vector3 bodyPos = start + (dir.normalized * (headWorldLen + bodyWorldLen * 0.5f));
                DrawSegment(mesh, mat, bodyPos, rot, thickness, bodyWorldLen, BodyUVLen, BodyUVOffset);
            }
        }

        private static void DrawSegment(
            Mesh mesh,
            Material mat,
            Vector3 pos,
            Quaternion rot,
            float width,
            float length,
            float uvHeightScale,
            float uvYOffset)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                rot,
                new Vector3(width, 1f, length)
            );

            propertyBlock.Clear();
            propertyBlock.SetVector("_MainTex_ST", new Vector4(1f, uvHeightScale, 0f, uvYOffset));
            propertyBlock.SetColor("_Color", mat.color);

            Graphics.DrawMesh(
                mesh,
                matrix,
                mat,
                0,              
                null,           
                0,              
                propertyBlock  
            );
        }
    }
}