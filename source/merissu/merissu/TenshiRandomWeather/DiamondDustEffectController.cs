using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class DiamondDustEffectController : GameComponent
    {
        private const string TargetWeatherDef = "DiamondDust";

        private static readonly Material DiamondDustMat = MaterialPool.MatFrom("weather/weatherM000", ShaderDatabase.Mote, Color.white);
        private static readonly Texture2D ParticleTexA = ContentFinder<Texture2D>.Get("weather/weatherQ000");
        private static readonly Texture2D ParticleTexB = ContentFinder<Texture2D>.Get("weather/weatherQ001");

        private const float FogScrollSpeed = 0.04f;

        private List<SimpleParticle> particlesA = new List<SimpleParticle>();
        private List<SimpleParticle> particlesB = new List<SimpleParticle>();

        private const int ParticleCount = 3;

        public DiamondDustEffectController(Game game) : base()
        {
            InitParticles();
        }

        private void InitParticles()
        {
            particlesA.Clear();
            particlesB.Clear();
            for (int i = 0; i < ParticleCount; i++)
            {
                particlesA.Add(new SimpleParticle(true));
                particlesB.Add(new SimpleParticle(false));
            }
        }

        public override void GameComponentOnGUI()
        {
            if (Find.CurrentMap?.weatherManager?.curWeather?.defName != TargetWeatherDef) return;

            DrawDiamondDustOverlay();

            DrawParticles(particlesA, ParticleTexA, 0.35f);
            DrawParticles(particlesB, ParticleTexB, 0.55f);
        }

        private void DrawDiamondDustOverlay()
        {
            float drawWidth = UI.screenWidth;
            if (DiamondDustMat.mainTexture == null) return;

            float aspectRatio = (float)DiamondDustMat.mainTexture.height / DiamondDustMat.mainTexture.width;
            float drawHeight = drawWidth * aspectRatio;
            float screenY = UI.screenHeight - drawHeight;

            float uvOffset = (RealTime.LastRealTime * FogScrollSpeed) % 1f;
            Rect uvRect = new Rect(-uvOffset, 0.01f, 1f, 0.98f);

            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTextureWithTexCoords(new Rect(0, screenY, drawWidth, drawHeight), DiamondDustMat.mainTexture, uvRect);
            GUI.color = Color.white;
        }

        private void DrawParticles(List<SimpleParticle> pList, Texture2D tex, float alpha)
        {
            if (tex == null) return;
            float deltaTime = Time.deltaTime;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            for (int i = 0; i < pList.Count; i++)
            {
                SimpleParticle p = pList[i];

                p.pos.x += p.speed * deltaTime * 250f; 
                p.pos.y += Mathf.Sin(RealTime.LastRealTime * 0.4f + i) * 0.3f;

                if (p.pos.x > UI.screenWidth)
                {
                    p.pos.x = -p.size;
                    p.pos.y = Random.Range(UI.screenHeight * 0.65f, UI.screenHeight * 0.95f);
                }

                GUI.DrawTexture(new Rect(p.pos.x, p.pos.y, p.size, p.size), tex);
            }
            GUI.color = Color.white;
        }

        private class SimpleParticle
        {
            public Vector2 pos;
            public float speed;
            public float size;

            public SimpleParticle(bool isTypeA)
            {
                pos = new Vector2(Random.Range(0, UI.screenWidth), Random.Range(UI.screenHeight * 0.65f, UI.screenHeight * 0.95f));

                if (isTypeA) 
                {
                    speed = Random.Range(1.8f, 3.0f);
                    size = Random.Range(45f, 75f);
                }
                else 
                {
                    speed = Random.Range(2.5f, 4.0f);
                    size = Random.Range(90f, 140f);
                }
            }
        }
    }
}