using RimWorld;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{

    public class Mote_AyaRedGhost : MoteThrown
    {
        public Texture snapshotTex;
        private static Material cachedMat;
        private MaterialPropertyBlock mpb;
        private float yOffset;
        public float targetScale = 1f;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            yOffset = Rand.Range(0f, 0.02f);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (snapshotTex == null) return;

            if (cachedMat == null)
            {
                Shader shader = DefDatabase<ShaderTypeDef>.GetNamed("merissu_RedGhost").Shader;
                cachedMat = new Material(shader ?? ShaderDatabase.Mote);
            }

            if (mpb == null) mpb = new MaterialPropertyBlock();

            float currentAlpha = this.Alpha;

            float visualAlpha = currentAlpha * currentAlpha;

            mpb.SetTexture("_MainTex", snapshotTex);
            mpb.SetFloat("_AgeSecs", this.AgeSecs);
            mpb.SetFloat("_Alpha", visualAlpha); 
            mpb.SetFloat("_RandomPerObject", (this.thingIDNumber % 100) / 100f);
            mpb.SetColor("_Color", Color.red);

            mpb.SetFloat("_GlowIntensity", 2.5f * visualAlpha);

            float s = targetScale * 1.35f;
            Vector3 finalScale = new Vector3(s, 1f, s);
            Vector3 pos = drawLoc;

            pos.y = AltitudeLayer.Pawn.AltitudeFor() - 0.05f + yOffset;
            Quaternion rot = Quaternion.identity;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, finalScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, cachedMat, 0, null, 0, mpb);
        }
    }
    [StaticConstructorOnStartup]
    public static class AyaShaderLoader
    {
        static AyaShaderLoader()
        {
            var mod = ModLister.GetActiveModWithIdentifier("merissu.AyaCard");
            if (mod == null) return;

            string bundlePath = Path.Combine(mod.RootDir.FullName, "Bundles/ayashader");
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle != null)
            {
                Shader loadedShader = bundle.LoadAsset<Shader>("merissu/RW_Mote_CyberRedGhost");

                if (loadedShader != null)
                {
                    ShaderTypeDef targetDef = DefDatabase<ShaderTypeDef>.GetNamed("merissu_RedGhost");

                    if (targetDef != null)
                    {
                        FieldInfo field = typeof(ShaderTypeDef).GetField("shader", BindingFlags.Instance | BindingFlags.NonPublic);

                        if (field != null)
                        {
                            field.SetValue(targetDef, loadedShader);
                            Log.Message("[AyaCard] 成功将 AssetBundle 中的 Shader 注入到 ShaderTypeDef: merissu_RedGhost");
                        }
                        else
                        {
                            Log.Error("[AyaCard] 反射失败：找不到 ShaderTypeDef 中的 shader 字段");
                        }
                    }
                }
                else
                {
                    Log.Error("[AyaCard] Bundle 加载成功但找不到 Shader 资源");
                }
            }
        }
    } 
    public class HediffCompProperties_AyaRedGhost : HediffCompProperties
    {
        public HediffCompProperties_AyaRedGhost()
        {
            this.compClass = typeof(HediffComp_AyaRedGhost);
        }
    }

    public class HediffComp_AyaRedGhost : HediffComp
    {
        private int lastMoveTick = -9999;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!Pawn.Spawned) return;

            if (Pawn.Drafted)
            {
                parent.Severity = 1.0f;
            }
            else
            {
                parent.Severity = 0.4f;
            }
            int currentTick = Find.TickManager.TicksGame;

            if (Pawn.pather.Moving)
            {
                lastMoveTick = currentTick;
            }

            bool allowGhost = Pawn.Drafted && (Pawn.pather.Moving || (currentTick - lastMoveTick <= 30));

            if (allowGhost)
            {
                if (currentTick % 2 == 0)
                {
                    ThrowAyaGhost(Pawn);
                }
            }
        }
        private void ThrowAyaGhost(Pawn pawn)
        {
            Mote_AyaRedGhost mote = (Mote_AyaRedGhost)ThingMaker.MakeThing(ThingDef.Named("Mote_AyaRedGhost"));
            if (mote == null) return;

            mote.exactPosition = pawn.DrawPos;
            mote.exactRotation = pawn.Rotation.AsAngle;

            mote.targetScale = pawn.ageTracker.CurLifeStage.bodySizeFactor;

            RenderTexture snapshot = PortraitsCache.Get(pawn, new Vector2(128f, 128f), pawn.Rotation, default, 1.2f);
            mote.snapshotTex = snapshot;

            GenSpawn.Spawn(mote, pawn.Position, pawn.Map);
        }
    }
}