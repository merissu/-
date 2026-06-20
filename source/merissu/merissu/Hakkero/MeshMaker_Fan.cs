using System.Collections.Generic;
using UnityEngine;

namespace merissu
{
    public static class MeshMaker_Fan
    {
        private static readonly Dictionary<int, Mesh> Cache =
            new Dictionary<int, Mesh>();

        public static Mesh GetFanMesh(float angle, int segments = 50)
        {
            int key = Mathf.RoundToInt(angle * 100f);

            if (Cache.TryGetValue(key, out Mesh mesh))
                return mesh;

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;

            float halfAngle = angle * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    i / (float)segments);

                float rad = a * Mathf.Deg2Rad;

                vertices[i + 1] =
                    new Vector3(
                        Mathf.Sin(rad),
                        0f,
                        Mathf.Cos(rad));
            }

            int t = 0;

            for (int i = 1; i <= segments; i++)
            {
                triangles[t++] = 0;
                triangles[t++] = i;
                triangles[t++] = i + 1;
            }

            mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles
            };

            mesh.RecalculateBounds();

            Cache[key] = mesh;

            return mesh;
        }
    }
}