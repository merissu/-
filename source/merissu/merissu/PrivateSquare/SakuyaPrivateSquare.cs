using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound; 

namespace merissu
{
    public class PrivateSquareVisual : Thing
    {
        protected override void Tick()
        {
            if (!PrivateSquareManager.IsActive)
            {
                this.Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (this.Map == null) return;

            float mapX = (float)this.Map.Size.x;
            float mapZ = (float)this.Map.Size.z;
            float alt = Altitudes.AltitudeFor(AltitudeLayer.Floor);
            Vector3 center = new Vector3(mapX / 2f, alt, mapZ / 2f);

            Matrix4x4 matrix = default;
            matrix.SetTRS(center, Quaternion.identity, new Vector3(mapX, 1f, mapZ));

            float breath = (Mathf.Sin(Time.realtimeSinceStartup * 2.0f) + 1f) * 0.25f;

            Material mat = this.Graphic.MatSingle;
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            block.SetColor("_Color", new Color(1f, 1f, 1f, breath));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, block);
        }
    }

    [StaticConstructorOnStartup]
    public static class PrivateSquareManager
    {
        public static bool IsActive = false;
        public static Pawn Caster = null;
        public static int DurationTicks = 0;
        public static float VisualTimeCounter = 0f;

        public static void Activate(Pawn pawn, int duration)
        {
            IsActive = true;
            Caster = pawn;
            DurationTicks = duration;
            VisualTimeCounter = Time.timeSinceLevelLoad;

            SoundDef theWorldSound = SoundDef.Named("theworld");
            if (theWorldSound != null)
            {
                theWorldSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            if (pawn.health != null)
            {
                string[] hediffNames = { "Private", "Square" };
                foreach (string name in hediffNames)
                {
                    HediffDef hdDef = HediffDef.Named(name);
                    if (hdDef != null && !pawn.health.hediffSet.HasHediff(hdDef))
                    {
                        pawn.health.AddHediff(hdDef);
                    }
                }
            }

            if (pawn.Map != null)
            {
                ThingDef visualDef = DefDatabase<ThingDef>.GetNamed("Sakuya_PrivateSquare", false);
                if (visualDef != null)
                {
                    GenSpawn.Spawn(visualDef, pawn.Map.Center, pawn.Map);
                }
            }

            Messages.Message("时符「Private Square」", pawn, MessageTypeDefOf.NeutralEvent);
        }

        public static void Deactivate()
        {
            if (IsActive)
            {
                Messages.Message("时间流速恢复正常", MessageTypeDefOf.PositiveEvent);
            }

            IsActive = false;
            Caster = null;
            DurationTicks = 0;
        }
    }

    public class SakuyaPrivateSquare : Ability
    {
        public SakuyaPrivateSquare() : base() { }
        public SakuyaPrivateSquare(Pawn pawn) : base(pawn) { }
        public SakuyaPrivateSquare(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足";
                }
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp != null && hp.Severity >= 1f)
            {
                hp.Severity -= 1f; 

                PrivateSquareManager.Activate(pawn, 10000);
                return base.Activate(target, dest);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(WeatherManager), "WeatherManagerTick")]
    public static class Patch_WeatherLogic_Slow
    {
        public static bool Prefix()
        {
            if (PrivateSquareManager.IsActive) return Find.TickManager.TicksGame % 5 == 0;
            return true;
        }
    }

    [HarmonyPatch(typeof(Game), "UpdatePlay")]
    public static class Patch_WeatherRender_Slow
    {
        public static void Postfix()
        {
            if (PrivateSquareManager.IsActive)
            {
                PrivateSquareManager.VisualTimeCounter += Time.deltaTime * 0.2f;
                Shader.SetGlobalFloat(ShaderPropertyIDs.GameSeconds, PrivateSquareManager.VisualTimeCounter);
            }
            else
            {
                PrivateSquareManager.VisualTimeCounter = Time.timeSinceLevelLoad;
            }
        }
    }

    [HarmonyPatch(typeof(WindManager), "get_WindSpeed")]
    public static class Patch_WindSpeed_Slow
    {
        public static void Postfix(ref float __result)
        {
            if (PrivateSquareManager.IsActive) __result *= 0.2f;
        }
    }

    [HarmonyPatch(typeof(Projectile), "Tick")]
    public static class Patch_Projectile_DeepDilation
    {
        public static bool Prefix(Projectile __instance)
        {
            if (!PrivateSquareManager.IsActive) return true;
            if (Find.TickManager.TicksGame % 5 != 0)
            {
                var tr = Traverse.Create(__instance);
                int ticksToImpact = tr.Field("ticksToImpact").GetValue<int>();
                if (ticksToImpact > 0) tr.Field("ticksToImpact").SetValue(ticksToImpact + 1);
                int launchTick = tr.Field("launchTick").GetValue<int>();
                tr.Field("launchTick").SetValue(launchTick + 1);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_SlowDown
    {
        public static bool Prefix(Pawn __instance)
        {
            if (!PrivateSquareManager.IsActive) return true;
            if (__instance == PrivateSquareManager.Caster) return true;
            return Find.TickManager.TicksGame % 5 == 0;
        }
    }

    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class Patch_SlowMode_Timer
    {
        public static void Postfix()
        {
            if (PrivateSquareManager.IsActive)
            {
                PrivateSquareManager.DurationTicks--;
                if (PrivateSquareManager.DurationTicks <= 0)
                {
                    PrivateSquareManager.Deactivate();
                }
            }
        }
    }
}