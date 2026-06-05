using UnityEngine;
using Verse;
using HarmonyLib;

namespace merissu
{
    public static class STG_HitManager
    {
        public static bool IsForcingHit = false;

        public const float HitboxHalfWidth = 0.1f;
        public const float GrazeHalfWidth = 0.8f;     

        public static bool SegmentIntersectsHitbox(Vector3 p1, Vector3 p2, Vector3 center, float halfSize)
        {
            Vector2 a = new Vector2(p1.x, p1.z);
            Vector2 b = new Vector2(p2.x, p2.z);
            Vector2 min = new Vector2(center.x - halfSize, center.z - halfSize);
            Vector2 max = new Vector2(center.x + halfSize, center.z + halfSize);

            if (Mathf.Min(a.x, b.x) > max.x || Mathf.Max(a.x, b.x) < min.x ||
                Mathf.Min(a.y, b.y) > max.y || Mathf.Max(a.y, b.y) < min.y)
            {
                return false;
            }

            if (a.x >= min.x && a.x <= max.x && a.y >= min.y && a.y <= max.y) return true;
            if (b.x >= min.x && b.x <= max.x && b.y >= min.y && b.y <= max.y) return true;

            Vector2 dir = b - a;
            float tMinX = (min.x - a.x) / (dir.x != 0 ? dir.x : 0.00001f);
            float tMaxX = (max.x - a.x) / (dir.x != 0 ? dir.x : 0.00001f);
            float tMinY = (min.y - a.y) / (dir.y != 0 ? dir.y : 0.00001f);
            float tMaxY = (max.y - a.y) / (dir.y != 0 ? dir.y : 0.00001f);

            if (tMinX > tMaxX) Swap(ref tMinX, ref tMaxX);
            if (tMinY > tMaxY) Swap(ref tMinY, ref tMaxY);

            float tEnter = Mathf.Max(tMinX, tMinY);
            float tExit = Mathf.Min(tMaxX, tMaxY);

            return tEnter <= tExit && tEnter <= 1f && tExit >= 0f;
        }
        public class GrazeParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public float age = 0f;
            public float maxAge = 1.0f;       
            private bool isHoming = false;

            private const float BaseScale = 0.16f;     
            private const float HomingDelay = 0.5f;   
            private const float AirResistance = 2f;  

            public GrazeParticle(Vector3 spawnPos)
            {
                this.position = spawnPos;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(3.0f, 5.0f);
                this.velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * speed;
            }

            public bool Update(Vector3 playerPos, float deltaTime)
            {
                age += deltaTime;
                if (age > maxAge) return false;

                if (age > HomingDelay) isHoming = true;

                if (isHoming)
                {
                    Vector3 dir = (playerPos - position).Yto0();
                    float dist = dir.magnitude;

                    if (dist < 0.12f) return false;

                    float homingProgress = (age - HomingDelay) / (maxAge - HomingDelay);
                    float homeSpeed = Mathf.Lerp(1f, 5f, homingProgress);

                    position += dir.normalized * homeSpeed * deltaTime;
                }
                else
                {
                    position += velocity * deltaTime;
                    velocity *= Mathf.Exp(-AirResistance * deltaTime);
                }

                return true;
            }

            public void Draw(Material mat, Mesh mesh)
            {
                float scale = Mathf.Min(1f, (maxAge - age) * 3f) * BaseScale;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    position + new Vector3(0, 0.15f, 0), 
                    Quaternion.identity,
                    new Vector3(scale, 1f, scale)
                );

                Graphics.DrawMesh(mesh, matrix, mat, 0);
            }
        }
        private static void Swap(ref float a, ref float b)
        {
            float temp = a;
            a = b;
            b = temp;
        }
    }
}