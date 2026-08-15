using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

            YayoAnimation_Compat_Patch.Initialize();
        }
        public override string SettingsCategory() => "咲夜的世界";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("暂停投射物", ref Settings.pauseProjectiles);
            listing.CheckboxLabeled("暂停所有动画 (水面/火焰/翅膀)", ref Settings.pauseAnimations);
            listing.End();
            Settings.Write();
        }
    }
    [StaticConstructorOnStartup]

    public static class YayoAnimation_Compat_Patch
    {
        private static bool initialized;

        private static int slowStartTick;
        public static IEnumerable<CodeInstruction> Transpiler_AniMovement(
    IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo ticksGameGetter =
                AccessTools.PropertyGetter(
                    typeof(TickManager),
                    nameof(TickManager.TicksGame));

            MethodInfo replacement =
                AccessTools.Method(
                    typeof(YayoAnimation_Compat_Patch),
                    nameof(GetYayoTicksGame),
                    new[] { typeof(TickManager) });

            if (ticksGameGetter == null ||
                replacement == null)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }

                yield break;
            }

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(ticksGameGetter))
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        replacement);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            Log.Message("[Merissu] 正在检查 Yayo Animation...");

            try
            {
                Type animationCoreType =
                    FindType("YayoAnimation.AnimationCore");

                if (animationCoreType == null)
                {
                    Log.Message(
                        "[Merissu] 未找到 YayoAnimation.AnimationCore，" +
                        "跳过 Yayo 兼容补丁。");
                    return;
                }

                Log.Message(
                    "[Merissu] 找到 YayoAnimation.AnimationCore，" +
                    "准备 Patch CheckAni。");

                MethodInfo checkAniMethod =
                    AccessTools.Method(
                        animationCoreType,
                        "CheckAni");

                if (checkAniMethod == null)
                {
                    Log.Warning(
                        "[Merissu] 找到 AnimationCore，" +
                        "但没有找到 CheckAni。");
                    return;
                }

                Harmony harmony =
                    new Harmony("merissu.yayoanimation.compat");

                harmony.Patch(
                    checkAniMethod,
                    prefix: new HarmonyMethod(
                        typeof(YayoAnimation_Compat_Patch),
                        nameof(Prefix_CheckAni)));

                MethodInfo aniMovementMethod =
                    AccessTools.Method(
                        animationCoreType,
                        "AniMovement");

                if (aniMovementMethod != null)
                {
                    harmony.Patch(
                        aniMovementMethod,
                        transpiler: new HarmonyMethod(
                            typeof(YayoAnimation_Compat_Patch),
                            nameof(Transpiler_AniMovement)));

                    Log.Message(
                        "[Merissu] Yayo AniMovement 时缓兼容补丁加载成功！");
                }
                Log.Message(
                    "[Merissu] Yayo Animation 时停 / 时缓兼容补丁加载成功！");
            }
            catch (Exception e)
            {
                Log.Error(
                    "[Merissu] Yayo Animation 兼容补丁加载失败:\n" +
                    e);
            }
        }

        private static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName);

            if (type != null)
                return type;

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    type = assembly.GetType(
                        fullName,
                        false);

                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }


        public static bool Prefix_CheckAni(Pawn pawn)
        {
            if (!TimeStopManager.IsTimeStopped)
                return true;

            if (SakuyaMod.Settings == null ||
                !SakuyaMod.Settings.pauseAnimations)
            {
                return true;
            }

            if (pawn == null)
                return true;

            if (pawn == TimeStopManager.TimeStopOwner)
                return true;

            if (TimeStopManager.IsProtected(pawn))
                return true;

            return false;
        }


        public static IEnumerable<CodeInstruction> Transpiler_CheckAni(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo ticksGameGetter =
                AccessTools.PropertyGetter(
                    typeof(TickManager),
                    nameof(TickManager.TicksGame));

            MethodInfo replacement =
                AccessTools.Method(
                    typeof(YayoAnimation_Compat_Patch),
                    nameof(GetYayoTicksGame),
                    new[] { typeof(TickManager) });

            if (ticksGameGetter == null ||
                replacement == null)
            {
                Log.Warning(
                    "[Merissu] Yayo Animation Transpiler 找不到 TicksGame getter 或替换方法。");

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }

                yield break;
            }

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(ticksGameGetter))
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        replacement);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        public static int GetYayoTicksGame(TickManager _)
        {
            if (TimeStopManager.IsTimeStopped)
            {
                return TimeStopManager.FrozenTick;
            }

            if (!PrivateSquareManager.IsActive)
            {
                return Find.TickManager.TicksGame;
            }

            int elapsed =
                Find.TickManager.TicksGame -
                slowStartTick;

            if (elapsed <= 0)
            {
                return slowStartTick;
            }

            return
                slowStartTick +
                elapsed / 5;
        }

        public static void StartSlowTime()
        {
            slowStartTick =
                Find.TickManager.TicksGame;

            Log.Message(
                $"[Merissu] Yayo 时缓开始，基准 Tick = {slowStartTick}");
        }

        public static void StopSlowTime()
        {
            slowStartTick = 0;

            Log.Message(
                "[Merissu] Yayo 时缓结束。");
        }
    }
    public class SakuyaSettings : ModSettings
    {
        public bool pauseProjectiles = true;
        public bool pauseAnimations = true;

        public override void ExposeData()
        {
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
            RemainingTicks = 999999;
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

            try
            {
                if (TimeStopOwner != null && TimeStopOwner.health != null)
                {
                    Hediff def = TimeStopOwner.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("the"));
                    if (def != null)
                    {
                        TimeStopOwner.health.RemoveHediff(def);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("移除时停状态时发生错误: " + e.Message);
            }

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
            if (!IsTimeStopped || thing == null) return true;
            if (thing is Building_Door) return true;
            if (thing == TimeStopOwner) return true;

            IThingHolder parentHolder = thing.ParentHolder;
            if (parentHolder is Pawn_ApparelTracker a && a.pawn == TimeStopOwner) return true;
            if (parentHolder is Pawn_EquipmentTracker e && e.pawn == TimeStopOwner) return true;

            if (thing is Projectile p && p.Launcher == TimeStopOwner && !SakuyaMod.Settings.pauseProjectiles) return true;
            return false;
        }
    }
    [StaticConstructorOnStartup]

    public class TimeStopVisual : Thing
    {
        private static MaterialPropertyBlock blockCache;

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

            if (blockCache == null)
            {
                blockCache = new MaterialPropertyBlock();
            }

            blockCache.SetColor("_Color", new Color(1f, 1f, 1f, breath));

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, blockCache);
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
                if (TimeStopManager.IsTimeStopped)
                {
                    return AcceptanceReport.WasAccepted;
                }

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 5f)
                {
                    return "符卡不足 (需要5张)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (TimeStopManager.IsTimeStopped)
            {
                TimeStopManager.ResumeTime();
                return true;
            }

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 5f) return false;

            hp.Severity -= 5f;
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
        public static bool Prefix(Graphic_Flicker __instance, Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation, Graphic[] ___subGraphics)
        {
            if (!TimeStopManager.IsTimeStopped || !SakuyaMod.Settings.pauseAnimations) return true;

            if (___subGraphics != null && ___subGraphics.Length > 0)
            {
                float fireSize = 1f;
                if (thing is Fire fire) fireSize = fire.fireSize;
                int seed = thing.thingIDNumber ^ 80531001;
                int frozenIndex = Math.Abs((TimeStopManager.FrozenTick + seed) % ___subGraphics.Length);
                float sineWave = Mathf.Sin((float)seed + TimeStopManager.FrozenTime * 15f);
                float flickerScale = 0.85f + sineWave * 0.15f;
                Graphic graphic = ___subGraphics[frozenIndex];
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
        public static bool Prefix(PawnTweener __instance, Pawn ___pawn)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            return ___pawn == null || TimeStopManager.IsProtected(___pawn);
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
        public static bool Prefix(Pawn_RotationTracker __instance, Pawn ___pawn)
        {
            if (!TimeStopManager.IsTimeStopped) return true;
            return ___pawn == null || TimeStopManager.IsProtected(___pawn);
        }
    }

    [HarmonyPatch(typeof(TickList), "Tick")]
    public static class Patch_MainTick
    {
        public static bool Prefix(TickList __instance, TickerType ___tickType, List<List<Thing>> ___thingLists)
        {
            if (!TimeStopManager.IsTimeStopped) return true;

            int interval = ___tickType == TickerType.Normal ? 1 : ___tickType == TickerType.Rare ? 250 : 2000;
            var bucket = ___thingLists[Find.TickManager.TicksGame % interval];

            int count = bucket.Count;
            for (int i = 0; i < count; i++)
            {
                Thing thing = bucket[i];
                if (TimeStopManager.IsProtected(thing))
                {
                    thing.DoTick();
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class Patch_Timer
    {
        public static void Postfix()
        {
            if (TimeStopManager.IsTimeStopped)
            {
                Pawn owner = TimeStopManager.TimeStopOwner;

                if (owner == null || owner.Destroyed || owner.Dead || owner.Downed || owner.Map == null)
                {
                    TimeStopManager.ResumeTime();
                }
            }
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