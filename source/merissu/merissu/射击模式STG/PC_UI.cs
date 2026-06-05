using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public partial class PC
    {
        private void UpdateAndRenderGrazeParticles()
        {
            float deltaTime = Time.deltaTime;
            if (Find.TickManager.Paused) deltaTime = 0f;

            Vector3 playerPos = physicsPosition ?? pawn.DrawPos;

            for (int i = grazeParticles.Count - 1; i >= 0; i--)
            {
                if (grazeParticles[i].Update(playerPos, deltaTime))
                {
                    grazeParticles[i].Draw(GrazeParticleMat, MeshPool.plane10);
                }
                else
                {
                    grazeParticles.RemoveAt(i);
                }
            }

            if (pawn.IsHashIntervalTick(300) && grazedProjectileIds.Count > 100)
            {
                grazedProjectileIds.Clear();
            }
        }
        public void RenderPawn()
        {
            if (pawn.Map == null || !pawn.Spawned) return;

            LeanSmoothed = Vector3.SmoothDamp(LeanSmoothed, LeanTarget, ref _leanVelocity, 0.07f, 10f, Time.deltaTime);

            if (pawn.Drawer?.leaner != null)
            {
                IntVec3 snapped = IntVec3.Zero;
                if (LeanTarget.sqrMagnitude > 0.01f)
                {
                    snapped = Mathf.Abs(LeanTarget.x) >= Mathf.Abs(LeanTarget.z)
                        ? (LeanTarget.x > 0 ? IntVec3.East : new IntVec3(-1, 0, 0))
                        : (LeanTarget.z > 0 ? IntVec3.North : IntVec3.South);
                }
                pawn.Drawer.leaner.shootSourceOffset = snapped;
            }

            if (!physicsPosition.HasValue) return;
            var tweener = pawn.Drawer.tweener;
            tweener.lastTickSpringPos = tweener.tweenedPos;
            tweener.tweenedPos = physicsPosition.Value;
            tweener.lastDrawFrame = RealTime.frameCount;
            tweener.lastDrawTick = GenTicks.TicksGame;
            float targetAlpha = isSneaking ? MaxHitboxAlpha : 0f;
            hitboxCurrentAlpha = Mathf.MoveTowards(hitboxCurrentAlpha, targetAlpha, Time.deltaTime * FadeSpeed);

            if (isSneaking && !wasSneakingLastFrame)
            {
                hitboxAppearProgress = 0f;
            }
            wasSneakingLastFrame = isSneaking;

            if (hitboxCurrentAlpha > 0.001f)
            {
                hitboxAppearProgress += Time.deltaTime;
                hitboxRotation += Time.deltaTime * 60f;
                hitboxRotation %= 360f;

                float currentScale = 1.5f + 0.8f * Mathf.Exp(-hitboxAppearProgress * 15f);
                Vector3 centerPos = physicsPosition ?? pawn.DrawPos;

                float baseEffectAltitude = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

                float rotatingAlpha = hitboxCurrentAlpha;
                float staticAlpha = Mathf.Max(0f, hitboxCurrentAlpha - 0.05f);

                _staticPropBlock.SetColor("_Color", new Color(1f, 1f, 1f, staticAlpha));

                Vector3 staticPos = centerPos;
                staticPos.y = baseEffectAltitude; 

                Matrix4x4 staticMatrix = Matrix4x4.TRS(
                    staticPos,
                    Quaternion.identity,
                    new Vector3(currentScale, 1f, currentScale)
                );
                Graphics.DrawMesh(MeshPool.plane10, staticMatrix, ManualControlTextures.HitboxMat, 0, null, 0, _staticPropBlock);

                _rotatingPropBlock.SetColor("_Color", new Color(1f, 1f, 1f, rotatingAlpha));

                Vector3 rotatingPos = centerPos;
                rotatingPos.y = baseEffectAltitude + 0.005f;

                Matrix4x4 rotatingMatrix = Matrix4x4.TRS(
                    rotatingPos,
                    Quaternion.AngleAxis(hitboxRotation, Vector3.up),
                    new Vector3(currentScale, 1f, currentScale)
                );
                Graphics.DrawMesh(MeshPool.plane10, rotatingMatrix, ManualControlTextures.HitboxMat, 0, null, 0, _rotatingPropBlock);
                UpdateAndRenderGrazeParticles();
            }
        }
    }
}
