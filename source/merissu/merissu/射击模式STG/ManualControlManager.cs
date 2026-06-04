using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class ManualControlTextures
    {
        public static readonly Texture2D IconOff = ContentFinder<Texture2D>.Get("UI/STG/Control_Off", false) ?? BaseContent.BadTex;

        public static readonly Texture2D IconOn = ContentFinder<Texture2D>.Get("UI/STG/Control_On", false) ?? BaseContent.BadTex;
        public static readonly Texture2D zoom = ContentFinder<Texture2D>.Get("UI/STG/zoom", false) ?? BaseContent.BadTex;

    }

    public static class ManualControlManager
    {
        public static bool enabled;
        public static Pawn controlledPawn;
        private static int lastTriggerFrame = -1;

        public static void SetControl(Pawn pawn)
        {
            if (pawn == null) return;

            if (Time.frameCount == lastTriggerFrame) return;
            lastTriggerFrame = Time.frameCount;

            if (!pawn.Drafted)
            {
                Messages.Message(
                  "必须进入征召状态才能开启自机模式",
                  pawn,
                  MessageTypeDefOf.RejectInput
                );
                return;
            }

            if (enabled && controlledPawn == pawn)
            {
                ClearControl(pawn);
                return;
            }

            Pawn old = controlledPawn;
            enabled = true;
            controlledPawn = pawn;

            State.SetPC(pawn, false);
            if (Find.CameraDriver != null)
            {
                Find.CameraDriver.config = new STGCamera();
            }
            if (old != pawn)
            {
                if (old != null)
                {
                    Messages.Message($"已切换自机：{old.LabelShort} → {pawn.LabelShort}", pawn, MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    Messages.Message($"已开启自机模式：{pawn.LabelShort},方向键移动,shift进行低速移动", pawn, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        public static void ClearControl(Pawn pawn = null)
        {
            if (!enabled && controlledPawn == null) return;

            enabled = false;
            controlledPawn = null;

            State.ClearPC();

            Messages.Message("已退出自机模式", pawn, MessageTypeDefOf.NeutralEvent);
        }

        public static bool IsControlled(Pawn pawn)
        {
            return enabled && controlledPawn == pawn;
        }

        public static void Tick()
        {
            if (!enabled || controlledPawn == null) return;

            if (!controlledPawn.Spawned || controlledPawn.Dead)
            {
                ClearControl(controlledPawn);
                return;
            }

            if (!controlledPawn.Drafted)
            {
                Messages.Message($"{controlledPawn.LabelShort} 已退出征召状态，自机模式关闭", controlledPawn, MessageTypeDefOf.NegativeEvent);
                ClearControl(controlledPawn);
            }
        }
        public static void ForceReset()
        {
            enabled = false;
            controlledPawn = null;
            lastTriggerFrame = -1;

            State.PC = null;
            State.CameraLockPosition = null;
            State.skipDialog = false;

            if (Find.CameraDriver != null)
            {
                Find.CameraDriver.config = new CameraMapConfig_Normal();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_ManualControl
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result)
                yield return g;

            if (__instance == null || __instance.Dead || !__instance.Drafted)
                yield break;

            bool isCurrentControlled = ManualControlManager.IsControlled(__instance);

            Texture2D currentIcon = isCurrentControlled ? ManualControlTextures.IconOn : ManualControlTextures.IconOff;

            yield return new Command_Toggle
            {
                defaultLabel = "自机模式",
                defaultDesc = "进入自机模式,方向键移动,shift进行低速移动",

                icon = currentIcon,
                hotKey = STGKeyDefOf.ToggleManualControl, 

                isActive = () => isCurrentControlled,
                toggleAction = () =>
                {
                    ManualControlManager.SetControl(__instance);
                }
            };
            if (isCurrentControlled)
            {
                yield return new Command_Action
                {
                    defaultLabel = "切换视角",
                    defaultDesc = "循环切换视角",
                    icon = ManualControlTextures.zoom,

                    hotKey = STGKeyDefOf.ToggleCameraZoom,

                    action = () =>
                    {
                        if (Find.CameraDriver.config is STGCamera)
                        {
                            STGCamera.CycleZoom();
                        }
                    }
                };
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_Tick_ManualControl
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == ManualControlManager.controlledPawn)
            {
                ManualControlManager.Tick();
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class ManualControlInit
    {
        static ManualControlInit()
        {
            Log.Message("自机模式加载成功");
        }
    }
}
