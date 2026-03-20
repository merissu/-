using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class SakuyaMod : Mod
    {
        public static SakuyaSettings Settings;

        public SakuyaMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SakuyaSettings>();
            Harmony harmony = new Harmony("merissu.sakuya.world.fixed.v1.7_final_true_time");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory() => "咲夜的世界";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label($"持续时间: {Settings.duration} tick");
            Settings.duration = (int)listing.Slider(Settings.duration, 60f, 20000f);
            listing.CheckboxLabeled("暂停投射物", ref Settings.pauseProjectiles);
            listing.CheckboxLabeled("暂停所有动画 (水面/火焰/翅膀)", ref Settings.pauseAnimations);
            listing.End();
            Settings.Write();
        }
    }

    public class SakuyaSettings : ModSettings
    {
        public int duration = 10000;
        public bool pauseProjectiles = true;
        public bool pauseAnimations = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref duration, "duration", 10000);
            Scribe_Values.Look(ref pauseProjectiles, "pauseProjectiles", true);
            Scribe_Values.Look(ref pauseAnimations, "pauseAnimations", true);
        }
    }

    public struct FrozenGunState
    {
        public Vector3 drawLoc;
        public float aimAngle;
        public FrozenGunState(Vector3 loc, float angle)
        {
            drawLoc = loc;
            aimAngle = angle;
        }
    }

    public struct FrozenWorldState
    {
        public float celestialGlow;
        public GenCelestial.LightInfo frozenShadowInfo;
        public int startTicksGame;
        public int startTicksAbs;
        public string dateStringCached;
    }

    [StaticConstructorOnStartup]
    public static class TimeStopManager
    {
        public static bool IsTimeStopped;
        public static Pawn TimeStopOwner;
        public static int RemainingTicks;
        public static float FrozenTime;
        public static int FrozenTick;
        public static Dictionary<int, FrozenGunState> FrozenGuns = new Dictionary<int, FrozenGunState>();

        public static FrozenWorldState EnvState;

        public static void ActivateTheWorld(Pawn caster)
        {
            EnvState = new FrozenWorldState
            {
                startTicksGame = Find.TickManager.TicksGame,
                startTicksAbs = Find.TickManager.TicksAbs
            };
            if (caster.Map != null)
            {
                EnvState.frozenShadowInfo = GenCelestial.GetLightSourceInfo(caster.Map, GenCelestial.LightType.Shadow);
                EnvState.celestialGlow = GenCelestial.CurCelestialSunGlow(caster.Map);
                Vector2 longLat = Find.WorldGrid.LongLatOf(caster.Map.Tile);
                EnvState.dateStringCached = GenDate.DateFullStringAt(EnvState.startTicksAbs, longLat);
            }

            IsTimeStopped = true;
            TimeStopOwner = caster;
            RemainingTicks = SakuyaMod.Settings.duration;
            FrozenTime = Time.timeSinceLevelLoad;
            FrozenTick = Find.TickManager.TicksGame;
            FrozenGuns.Clear();

            if (caster.Map != null)
            {
                GenSpawn.Spawn(SakuyaThingDefOf.Sakuya_TimeStopVisual, caster.Map.Center, caster.Map);
            }

            SoundDef theWorldSound = SoundDef.Named("theworld");
            if (theWorldSound != null)
            {
                theWorldSound.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
            }

            string[] hediffsToAdd = { "the", "world" };
            if (caster != null && caster.health != null)
            {
                foreach (string defName in hediffsToAdd)
                {
                    HediffDef def = DefDatabase<HediffDef>.GetNamed(defName, false);
                    if (def != null && !caster.health.hediffSet.HasHediff(def))
                    {
                        caster.health.AddHediff(def);
                    }
                }
            }

            Messages.Message("The World！时间已停止！", caster, MessageTypeDefOf.PositiveEvent, true);
            caster.Map?.mapDrawer.RegenerateEverythingNow();
        }

        public static void ResumeTime()
        {
            if (!IsTimeStopped) return;

            if (EnvState.startTicksGame > 0)
            {
                Find.TickManager.DebugSetTicksGame(EnvState.startTicksGame);
            }

            IsTimeStopped = false;
            TimeStopOwner = null;
            RemainingTicks = 0;
            FrozenGuns.Clear();

            Messages.Message("时间开始流动。", MessageTypeDefOf.NeutralEvent, false);
        }

        public static bool IsProtected(Thing thing)
        {
            if (!IsTimeStopped) return true;
            if (thing == null) return true;
            if (thing is Building_Door) return true;
            if (thing == TimeStopOwner) return true;
            if (thing.ParentHolder is Pawn_ApparelTracker a && a.pawn == TimeStopOwner) return true;
            if (thing.ParentHolder is Pawn_EquipmentTracker e && e.pawn == TimeStopOwner) return true;
            if (thing is Projectile p && p.Launcher == TimeStopOwner && !SakuyaMod.Settings.pauseProjectiles) return true;
            return false;
        }
    }

    public class TimeStopVisual : Thing
    {
        protected override void Tick()
        {
            if (!TimeStopManager.IsTimeStopped)
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

    [RimWorld.DefOf]
    public static class SakuyaThingDefOf
    {
        public static ThingDef Sakuya_TimeStopVisual;
    }

    public class SakuyaTheWorld : Ability
    {
        public SakuyaTheWorld() : base() { }

        public SakuyaTheWorld(Pawn pawn) : base(pawn) { }
        public SakuyaTheWorld(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 2f)
                {
                    return "灵力不足 (需要2层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 2f)
            {
                return false;
            }

            hp.Severity -= 2f;

            TimeStopManager.ActivateTheWorld(pawn);
            return base.Activate(target, dest);
        }
    }

    [HarmonyPatch(typeof(GlobalControlsUtility), "DoDate")]
    public static class Patch_UI_Clock_Freeze
    {
        public static bool Prefix(float leftX, float width, ref float curBaseY)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            Rect rect = new Rect(leftX, curBaseY - 26f, width, 26f);
            Text.Anchor = TextAnchor.UpperRight;
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            Widgets.Label(rect, TimeStopManager.EnvState.dateStringCached);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            curBaseY -= 26f;
            return false;
        }
    }

    [HarmonyPatch(typeof(GenCelestial), "GetLightSourceInfo")]
    public static class Patch_Shadow_Freeze
    {
        public static bool Prefix(Map map, GenCelestial.LightType type, ref GenCelestial.LightInfo __result)
        {
            if (TimeStopManager.IsTimeStopped && type == GenCelestial.LightType.Shadow)
            {
                __result = TimeStopManager.EnvState.frozenShadowInfo;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GenCelestial), "CurCelestialSunGlow")]
    public static class Patch_Sun_Glow_Freeze
    {
        public static bool Prefix(Map map, ref float __result)
        {
            if (TimeStopManager.IsTimeStopped)
            {
                __result = TimeStopManager.EnvState.celestialGlow;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SkyManager), "SkyManagerUpdate")]
    public static class Patch_Sky_Color_Freeze
    {
        public static bool Prefix() => !TimeStopManager.IsTimeStopped;
    }


    [HarmonyPatch(typeof(Stance), "StanceDraw")]
    public static class Patch_Stance_Draw { public static bool Prefix() => true; }

    [HarmonyPatch(typeof(Graphic_Flicker), "DrawWorker")]
    public static class Patch_Fire_Freeze
    {
        public static bool Prefix(Graphic_Flicker __instance, Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (!TimeStopManager.IsTimeStopped || !SakuyaMod.Settings.pauseAnimations) return true;
            var subGraphics = Traverse.Create(__instance).Field("subGraphics").GetValue<Graphic[]>();
            if (subGraphics != null && subGraphics.Length > 0)
            {
                float fireSize = 1f;
                if (thing is Fire fire) fireSize = fire.fireSize;
                int seed = thing.thingIDNumber ^ 80531001;
                int frozenIndex = Math.Abs((TimeStopManager.FrozenTick + seed) % subGraphics.Length);
                float sineWave = Mathf.Sin((float)seed + TimeStopManager.FrozenTime * 15f);
                float flickerScale = 0.85f + sineWave * 0.15f;
                Graphic graphic = subGraphics[frozenIndex];
                Vector2 originalDrawSize = graphic.drawSize;
                graphic.drawSize = originalDrawSize * fireSize * flickerScale;
                graphic.Draw(loc, rot, thing, extraRotation);
                graphic.drawSize = originalDrawSize;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Game), "UpdatePlay")]
    public static class Patch_GlobalTime_Freeze
    {
        public static void Postfix()
        {
            if (TimeStopManager.IsTimeStopped && SakuyaMod.Settings.pauseAnimations)
                Shader.SetGlobalFloat(ShaderPropertyIDs.GameSeconds, TimeStopManager.FrozenTime);
        }
    }

    [HarmonyPatch(typeof(WeatherManager), "WeatherManagerTick")]
    public static class Patch_Weather_Tick { public static bool Prefix() => !TimeStopManager.IsTimeStopped; }

    [HarmonyPatch(typeof(WindManager), "get_WindSpeed")]
    public static class Patch_WindFreeze { public static void Postfix(ref float __result) { if (TimeStopManager.IsTimeStopped) __result = 0f; } }

    [HarmonyPatch(typeof(Projectile), "Tick")]
    public static class Patch_Proj_Tick { public static bool Prefix(Projectile __instance) => !TimeStopManager.IsTimeStopped || TimeStopManager.IsProtected(__instance); }

    [HarmonyPatch(typeof(PawnTweener), "PreDrawPosCalculation")]
    public static class Patch_Tweener_Freeze
    {
        public static bool Prefix(PawnTweener __instance)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            Pawn p = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            return p == null || TimeStopManager.IsProtected(p);
        }
    }

    [HarmonyPatch(typeof(PawnDownedWiggler), nameof(PawnDownedWiggler.ProcessPostTickVisuals))]
    public static class Patch_DownedWiggler_Freeze
    {
        public static bool Prefix(PawnDownedWiggler __instance, int ticksPassed)
        {
            if (!TimeStopManager.IsTimeStopped)
                return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AnimationTick), MethodType.Getter)]
    public static class Patch_RenderTree_Animation_Freeze
    {
        public static bool Prefix(PawnRenderTree __instance, ref int __result)
        {
            if (!TimeStopManager.IsTimeStopped || !SakuyaMod.Settings.pauseAnimations)
                return true;
            Pawn pawn = __instance.pawn;
            if (pawn == null || TimeStopManager.IsProtected(pawn))
                return true;

            __result = 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_RotationTracker), "UpdateRotation")]
    public static class Patch_Rotation_Freeze
    {
        public static bool Prefix(Pawn_RotationTracker __instance)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            Pawn p = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            return p == null || TimeStopManager.IsProtected(p);
        }
    }

    [HarmonyPatch(typeof(TickList), "Tick")]
    public static class Patch_MainTick
    {
        public static bool Prefix(TickList __instance)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            var tr = Traverse.Create(__instance);
            var tickType = tr.Field("tickType").GetValue<TickerType>();
            var thingLists = tr.Field("thingLists").GetValue<List<List<Thing>>>();
            int interval = tickType == TickerType.Normal ? 1 : tickType == TickerType.Rare ? 250 : 2000;
            var bucket = thingLists[Find.TickManager.TicksGame % interval];
            for (int i = 0; i < bucket.Count; i++)
            {
                if (TimeStopManager.IsProtected(bucket[i])) bucket[i].DoTick();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class Patch_Timer
    {
        public static void Postfix()
        {
            if (!TimeStopManager.IsTimeStopped) return;
            TimeStopManager.RemainingTicks--;
            if (TimeStopManager.RemainingTicks <= 0) TimeStopManager.ResumeTime();
        }
    }

    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_Weapon_Aim_Freeze
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            if (!TimeStopManager.IsTimeStopped) return;
            Pawn p = (eq.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            if (p == null || TimeStopManager.IsProtected(p)) return;
            int gunID = eq.thingIDNumber;
            if (TimeStopManager.FrozenGuns.TryGetValue(gunID, out FrozenGunState state))
            {
                drawLoc = state.drawLoc;
                aimAngle = state.aimAngle;
            }
            else
            {
                TimeStopManager.FrozenGuns[gunID] = new FrozenGunState(drawLoc, aimAngle);
            }
        }
    }
}