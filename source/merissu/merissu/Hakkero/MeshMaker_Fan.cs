using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public static class MeshMaker_Fan
    {
        private static readonly Dictionary<float, Mesh> cache =
            new Dictionary<float, Mesh>();

        public static Mesh GetFanMesh(float angle, int segments = 50)
        {
            if (cache.TryGetValue(angle, out Mesh mesh))
                return mesh;

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(
                    -angle / 2f,
                     angle / 2f,
                     i / (float)segments);

                vertices[i + 1] =
                    Quaternion.Euler(0, a, 0) * Vector3.forward;
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

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            cache[angle] = mesh;
            return mesh;
        }
    }
}
