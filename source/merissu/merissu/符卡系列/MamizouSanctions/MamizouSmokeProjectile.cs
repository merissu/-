using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class MamizouSmokeProjectile : Thing
    {
        private Vector3 exactPosition;
        private Vector3 direction;
        private Pawn caster;

        private float distanceTraveled = 0f;
        private HashSet<Pawn> hitTargets = new HashSet<Pawn>();

        private const float MaxRange = 15f;
        private const float Speed = 0.3f;
        private const float MaxScale = 15f;

        private static readonly Material SmokeBMat = MaterialPool.MatFrom("Other/MamizouSanctions/smokeB", ShaderDatabase.MoteGlow);

        public void Launch(Pawn caster, IntVec3 target)
        {
            this.caster = caster;
            this.exactPosition = this.Position.ToVector3Shifted();
            Vector3 targetVec = target.ToVector3Shifted();
            this.direction = (targetVec - exactPosition).normalized;
        }

        protected override void Tick()
        {
            if (Destroyed) return;

            distanceTraveled += Speed;
            exactPosition += direction * Speed;
            IntVec3 newPos = exactPosition.ToIntVec3();

            if (newPos != Position && newPos.InBounds(Map))
            {
                Position = newPos;
            }

            float currentRadius = Mathf.Lerp(2f, 8f, distanceTraveled / MaxRange);
            List<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(Position, Map, currentRadius, true).ToList();

            foreach (Thing thing in thingsInRadius)
            {
                Pawn p = thing as Pawn;
                if (p != null && p != caster && !hitTargets.Contains(p) && GenHostility.HostileTo(p, caster))
                {
                    hitTargets.Add(p); 
                    HitTarget(p);
                }
            }

            if (distanceTraveled >= MaxRange || !exactPosition.ToIntVec3().InBounds(Map))
            {
                Destroy();
            }
        }

        private void HitTarget(Pawn targetPawn)
        {
            MamizouTransformManager manager = Current.Game.GetComponent<MamizouTransformManager>();

            if (manager.IsTransformed(targetPawn)) return;

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("TanukiPipeSanctions");
            if (soundDef != null)
            {
                soundDef.PlayOneShot(new TargetInfo(targetPawn.Position, targetPawn.Map));
            }

            manager.StartTransformation(targetPawn);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = distanceTraveled / MaxRange;
            float scale = Mathf.Lerp(0.5f, MaxScale, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);

            if (alpha <= 0) return;

            Material mat = FadedMaterialPool.FadedVersionOf(SmokeBMat, alpha);
            float baseAngle = distanceTraveled * 50f;

            for (int i = 0; i < 3; i++)
            {
                float angle = baseAngle + (i * 120f);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * 0.5f;
                Vector3 finalPos = exactPosition + offset;
                finalPos.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);

                Matrix4x4 matrix = Matrix4x4.TRS(finalPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }
    }

    public class Mote_MamizouHitSmoke : Thing
    {
        private Vector3 exactPosition;
        private Vector3 velocity;
        private float rotation;
        private float rotationSpeed;
        private int age;
        private const int FadeDuration = 60;

        public void Init(Vector3 center)
        {
            exactPosition = center;
            Vector2 randDir = Rand.InsideUnitCircle.normalized * Rand.Range(0.05f, 0.025f);
            velocity = new Vector3(randDir.x, 0, randDir.y);
            rotationSpeed = Rand.Range(-20f, 20f);
        }

        protected override void Tick()
        {
            age++;
            exactPosition += velocity;
            rotation += rotationSpeed;
            if (age >= FadeDuration) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - ((float)age / FadeDuration);
            if (alpha <= 0) return;

            Material mat = FadedMaterialPool.FadedVersionOf(Graphic.MatSingle, alpha);
            Vector3 drawPos = exactPosition;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(1.8f, 1f, 1.8f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}