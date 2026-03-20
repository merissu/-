using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using System.Collections.Generic;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class AbyssNovaUIController : GameComponent
    {
        private static bool active;
        private static float durationRemaining;
        private static bool isFadingIn;
        private static bool isFadingOut;
        private static float uiAlpha = 0f;

        private const float FADE_IN_SPEED = 2.0f;
        private const float FADE_OUT_SPEED = 1.5f;

        private static float centerOffset;
        private static float sideOffset;
        private static Sustainer sustainer;
        private static Texture2D centerTex;
        private static Texture2D sideTex;
        private static Pawn targetCaster;

        private static List<AbyssNovaParticle> particles = new List<AbyssNovaParticle>();
        private static Material particleMat;
        private const int PARTICLE_SPAWN_INTERVAL = 30;
        private static int particleTickCounter;

        private static float whiteScreenDuration = 0f;
        private const float TOTAL_WHITE_TIME = 3.0f;     
        private const float WHITE_FADE_OUT_START = 0.8f; 

        private static float shakeDurationRemaining = 0f;
        private const float TOTAL_SHAKE_TIME = 5.0f;     

        public AbyssNovaUIController(Game game) : base() { }

        public static void Start(float durationSeconds, Pawn caster)
        {
            if (caster == null || !caster.Spawned) return;

            active = true;
            isFadingIn = true;
            isFadingOut = false;
            uiAlpha = 0f;
            durationRemaining = durationSeconds;
            centerOffset = 0f;
            sideOffset = 0f;
            targetCaster = caster;

            centerTex = ContentFinder<Texture2D>.Get("Other/AbyssNova1");
            sideTex = ContentFinder<Texture2D>.Get("Other/AbyssNova2");
            particleMat = MaterialPool.MatFrom("Other/bulletFd002", ShaderDatabase.MoteGlow);
            particleMat.renderQueue = 4000;
            particles.Clear();
            particleTickCounter = 0;

            SoundDef sd = SoundDef.Named("AbyssNova");
            if (sd != null && sustainer == null)
            {
                sustainer = sd.TrySpawnSustainer(SoundInfo.OnCamera());
            }
        }

        public override void GameComponentOnGUI()
        {
            if (active)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, uiAlpha);
                DrawUI();
                GUI.color = oldColor;
            }

            if (whiteScreenDuration > 0)
            {
                float whiteAlpha = 1f;
                if (whiteScreenDuration < WHITE_FADE_OUT_START)
                {
                    whiteAlpha = whiteScreenDuration / WHITE_FADE_OUT_START;
                }
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, whiteAlpha);
                GUI.DrawTexture(new Rect(0, 0, UI.screenWidth, UI.screenHeight), BaseContent.WhiteTex);
                GUI.color = oldColor;
            }
        }

        public override void GameComponentUpdate()
        {
            if (whiteScreenDuration > 0 && !Find.TickManager.Paused)
            {
                whiteScreenDuration -= Time.deltaTime;
            }

            if (shakeDurationRemaining > 0 && !Find.TickManager.Paused)
            {
                shakeDurationRemaining -= Time.deltaTime;
                float currentShakeIntensity = Mathf.Lerp(0f, 12f, shakeDurationRemaining / TOTAL_SHAKE_TIME);
                Find.CameraDriver.shaker.DoShake(currentShakeIntensity);
            }

            if (!active) return;

            if (!Find.TickManager.Paused)
            {
                UpdateLogic();
            }

            if (active && particles.Count > 0)
            {
                foreach (var p in particles) p.Draw();
            }
            sustainer?.Maintain();
        }

        private void UpdateLogic()
        {
            if (isFadingIn)
            {
                uiAlpha += Time.deltaTime * FADE_IN_SPEED;
                if (uiAlpha >= 1f) { uiAlpha = 1f; isFadingIn = false; }
            }
            else if (isFadingOut)
            {
                uiAlpha -= Time.deltaTime * FADE_OUT_SPEED;
                if (uiAlpha <= 0f) { Stop(); return; }
            }
            else
            {
                durationRemaining -= Time.deltaTime;
                if (durationRemaining <= 0f)
                {
                    TriggerExplosion();
                    isFadingOut = true;
                    sustainer?.End();
                    sustainer = null;
                }
            }

            centerOffset += 250f * Time.deltaTime;
            sideOffset += 500f * Time.deltaTime;

            if (targetCaster != null && targetCaster.Spawned)
            {
                particleTickCounter++;
                if (particleTickCounter >= PARTICLE_SPAWN_INTERVAL)
                {
                    particleTickCounter = 0;
                    SpawnParticle(targetCaster);
                }
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Tick();
                if (particles[i].Dead) particles.RemoveAt(i);
            }
        }

        private static void TriggerExplosion()
        {
            if (targetCaster == null || !targetCaster.Spawned || targetCaster.Map == null) return;

            Map map = targetCaster.Map;
            IntVec3 center = targetCaster.Position;

            whiteScreenDuration = TOTAL_WHITE_TIME;
            shakeDurationRemaining = TOTAL_SHAKE_TIME;

            float vaporizeRadius = 50f;
            foreach (IntVec3 c in GenRadial.RadialCellsAround(center, vaporizeRadius, true))
            {
                if (!c.InBounds(map)) continue;

                if (Rand.Value < 0.4f)
                {
                    FleckMaker.ThrowSmoke(c.ToVector3Shifted(), map, Rand.Range(1.5f, 3.5f));
                    FleckMaker.Static(c, map, FleckDefOf.DustPuff, Rand.Range(1.2f, 2.8f));
                }

                List<Thing> thingList = c.GetThingList(map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    if (t == null) continue;

                    if (t == targetCaster) continue;

                    if (t.def.destroyable)
                    {
                        t.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            List<Thing> ignoredList = new List<Thing> { targetCaster };

            GenExplosion.DoExplosion(
                center: center,
                map: map,
                radius: 79f,
                damType: DamageDefOf.Bomb,
                instigator: targetCaster,
                damAmount: 5000,
                armorPenetration: 10f,
                explosionSound: SoundDef.Named("AbyssNova2"),
                ignoredThings: ignoredList, 
                damageFalloff: false
            );

            foreach (IntVec3 c in GenRadial.RadialCellsAround(center, 79f, true))
            {
                if (c.InBounds(map) && Rand.Value < 0.2f)
                {
                    FireUtility.TryStartFireIn(c, map, Rand.Range(0.1f, 0.9f), targetCaster);
                }
            }
        }
        private static void Stop()
        {
            active = false;
            isFadingIn = false;
            isFadingOut = false;
            uiAlpha = 0f;
            targetCaster = null;
            particles.Clear();
            sustainer?.End();
            sustainer = null;
        }

        private static void SpawnParticle(Pawn caster)
        {
            Vector3 basePos = caster.DrawPos;
            basePos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.1f;
            Vector2 rnd = Rand.InsideUnitCircle * Rand.Range(1.2f, 3f);
            Vector3 spawnPos = basePos + new Vector3(rnd.x, 0f, rnd.y);
            particles.Add(new AbyssNovaParticle
            {
                startPos = spawnPos,
                targetPos = basePos,
                life = 0,
                maxLife = Rand.Range(80, 120),
                size = Rand.Range(0.6f, 1.1f)
            });
        }

        private class AbyssNovaParticle
        {
            public Vector3 startPos, targetPos;
            public int life, maxLife;
            public float size;
            public void Tick() => life++;
            public void Draw()
            {
                float t = Mathf.Clamp01((float)life / maxLife);
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                pos.y = targetPos.y;
                particleMat.color = new Color(1f, 1f, 1f, 1f - t);
                Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size);
                Graphics.DrawMesh(MeshPool.plane10, matrix, particleMat, 0);
            }
            public bool Dead => life >= maxLife;
        }

        private static void DrawUI()
        {
            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;
            DrawScrolling(new Rect(0, screenH / 2f - 125f, screenW, 250f), centerTex, centerOffset, true, 50f);
            DrawScrolling(new Rect(0, 100f, screenW, 80f), sideTex, sideOffset, false, 0f);
            DrawScrolling(new Rect(0, screenH - 180f, screenW, 80f), sideTex, sideOffset, false, 0f);
        }

        private static void DrawScrolling(Rect rect, Texture2D tex, float offset, bool leftToRight, float gap)
        {
            if (tex == null) return;
            GUI.BeginGroup(rect);
            float cycleWidth = tex.width + gap;
            float effectiveOffset = offset % cycleWidth;
            float startX = leftToRight ? (effectiveOffset - cycleWidth) : -effectiveOffset;
            for (float x = startX; x < rect.width; x += cycleWidth)
            {
                GUI.DrawTexture(new Rect(x, 0, tex.width, rect.height), tex, ScaleMode.StretchToFill);
            }
            GUI.EndGroup();
        }
    }
}