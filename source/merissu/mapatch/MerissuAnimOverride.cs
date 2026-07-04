using AM;
using AM.Idle;
using AM.Reqs;
using AM.Tweaks;
using AM.UI;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace merissu
{
    public class MerissuAnimConfig : IExposable
    {
        public string defName;
        public List<string> forcedAnimDefNames = new();
        public List<ToolAnimBinding> toolBindings = new();

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Collections.Look(ref forcedAnimDefNames, "forcedAnimDefNames", LookMode.Value);
            Scribe_Collections.Look(ref toolBindings, "toolBindings", LookMode.Deep);
        }
    }

    public class ToolAnimBinding : IExposable
    {
        public int toolIndex;
        public string animDefName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref toolIndex, "toolIndex");
            Scribe_Values.Look(ref animDefName, "animDefName");
        }
    }

    [StaticConstructorOnStartup]
    public static class MerissuAnimOverrideManager
    {
        private static Dictionary<string, MerissuAnimConfig> _configs;

        private static string FilePath
        {
            get
            {
                var mod = LoadedModManager.RunningModsListForReading
                    .FirstOrDefault(m => m.PackageId == "touhou.merissu");
                if (mod == null) return null;
                return Path.Combine(mod.RootDir, "AnimOverrides", "MerissuAnimOverrides.xml");
            }
        }

        public static Dictionary<string, MerissuAnimConfig> Configs
        {
            get
            {
                if (_configs == null) Load();
                return _configs;
            }
        }

        public static MerissuAnimConfig GetOrCreate(string defName)
        {
            if (!Configs.TryGetValue(defName, out var cfg))
            {
                cfg = new MerissuAnimConfig { defName = defName };
                Configs[defName] = cfg;
            }
            return cfg;
        }

        public static void Save()
        {
            try
            {
                string path = FilePath;
                if (path == null)
                {
                    Log.Error("[Merissu] Cannot save: Mod not found.");
                    return;
                }

                var wrapper = new MerissuAnimConfigsWrapper
                {
                    configs = Configs.Values.ToList()
                };

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Scribe.saver.InitSaving(path, "root");
                wrapper.ExposeData();
                Scribe.saver.FinalizeSaving();
            }
            catch (Exception ex)
            {
                Log.Error($"[Merissu] Failed to save anim override config: {ex}");
            }
        }

        private static void Load()
        {
            _configs = new Dictionary<string, MerissuAnimConfig>();

            string path = FilePath;
            if (path == null || !File.Exists(path))
                return;

            try
            {
                var wrapper = new MerissuAnimConfigsWrapper();
                Scribe.loader.InitLoading(path);
                wrapper.ExposeData();
                Scribe.loader.FinalizeLoading();

                foreach (var cfg in wrapper.configs)
                    if (!cfg.defName.NullOrEmpty())
                        _configs[cfg.defName] = cfg;
            }
            catch (Exception ex)
            {
                Log.Error($"[Merissu] Failed to load anim override config: {ex}");
            }
        }

        private class MerissuAnimConfigsWrapper : IExposable
        {
            public List<MerissuAnimConfig> configs = new();
            public void ExposeData()
            {
                Scribe_Collections.Look(ref configs, "configs", LookMode.Deep);
            }
        }
    }

    [HarmonyPatch]
    public static class MerissuAnimOverridePatches
    {
        [ThreadStatic]
        private static int _currentToolIndex = -1;

        private static string _capturedToolLabel = null;
        private static string _capturedWeaponDefName = null;

        [HarmonyPatch]
        static class CaptureToolLabelPatch
        {
            [HarmonyPrepare]
            static bool Prepare() => AccessTools.TypeByName("CombatExtended.Verb_MeleeAttackCE") != null;

            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("CombatExtended.Verb_MeleeAttackCE");
                return AccessTools.Method(type, "DamageInfosToApply");
            }
            static void Prefix(object __instance)
            {
                try
                {
                    var toolField = __instance.GetType().GetField("tool", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (toolField != null)
                    {
                        var tool = toolField.GetValue(__instance) as Tool;
                        if (tool != null)
                        {
                            _capturedToolLabel = tool.label;
                            CaptureWeaponDef(__instance);
                            return;
                        }
                    }

                    var toolCEProp = __instance.GetType().GetProperty("ToolCE");
                    if (toolCEProp != null)
                    {
                        var toolCE = toolCEProp.GetValue(__instance);
                        if (toolCE is Tool t)
                        {
                            _capturedToolLabel = t.label;
                            CaptureWeaponDef(__instance);
                            return;
                        }
                    }
                }
                catch (Exception) { }

                _capturedToolLabel = null;
                _capturedWeaponDefName = null;
            }

            private static void CaptureWeaponDef(object verbInstance)
            {
                _capturedWeaponDefName = null;

                var eqProp = verbInstance.GetType().GetProperty("EquipmentSource");
                if (eqProp != null)
                {
                    var eq = eqProp.GetValue(verbInstance) as Thing;
                    if (eq?.def != null)
                    {
                        _capturedWeaponDefName = eq.def.defName;
                        return;
                    }
                }

                var pawnProp = verbInstance.GetType().GetProperty("CasterPawn");
                if (pawnProp != null)
                {
                    var pawn = pawnProp.GetValue(verbInstance) as Pawn;
                    if (pawn?.equipment?.Primary?.def != null)
                    {
                        _capturedWeaponDefName = pawn.equipment.Primary.def.defName;
                        return;
                    }
                }
            }
        }

        [HarmonyPatch]
        static class ClearToolLabelOnAttackStart
        {
            [HarmonyPrepare]
            static bool Prepare() => AccessTools.TypeByName("CombatExtended.Verb_MeleeAttackCE") != null;

            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("CombatExtended.Verb_MeleeAttackCE");
                return AccessTools.Method(type, "TryCastShot");
            }

            static void Prefix()
            {
                _capturedToolLabel = null;
                _capturedWeaponDefName = null;
            }
        }
        private static int GetToolIndex(Verb verb)
        {
            if (verb?.tool == null) return -1;

            var equipment = verb.EquipmentSource;
            if (equipment?.def?.tools != null)
            {
                int idx = equipment.def.tools.IndexOf(verb.tool);
                if (idx >= 0) return idx;
            }

            var hediffComp = verb.HediffCompSource;
            if (hediffComp?.Props?.tools != null)
            {
                int idx = hediffComp.Props.tools.IndexOf(verb.tool);
                if (idx >= 0) return idx;
            }

            return -1;
        }

        [HarmonyPatch(typeof(IdleControllerComp), "NotifyPawnDidMeleeAttack")]
        [HarmonyPriority(Priority.High)]
        static class Patch_NotifyPawnDidMeleeAttack
        {
            static void Prefix(Verb_MeleeAttack verbUsed)
            {
                _currentToolIndex = -1;

                if (!_capturedToolLabel.NullOrEmpty() && !_capturedWeaponDefName.NullOrEmpty())
                {
                    ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(_capturedWeaponDefName);
                    if (weaponDef?.tools != null)
                    {
                        for (int i = 0; i < weaponDef.tools.Count; i++)
                        {
                            if (weaponDef.tools[i].label == _capturedToolLabel)
                            {
                                _currentToolIndex = i;
                                return;
                            }
                        }
                    }
                }

                _currentToolIndex = GetToolIndex(verbUsed);
            }
        }
        [HarmonyPatch(typeof(ItemTweakData), "GetAttackAnimations")]
        public static class Patch_GetAttackAnimations
        {
            [HarmonyPostfix]
            static void Postfix(ItemTweakData __instance, ref IReadOnlyList<AnimDef> __result, Rot4 direction)
            {
                ThingDef weaponDef = __instance.GetDef();
                if (weaponDef == null) return;

                var config = MerissuAnimOverrideManager.GetOrCreate(weaponDef.defName);

                if (config.forcedAnimDefNames.Count == 0 && config.toolBindings.Count == 0)
                    return;

                int toolIdx = _currentToolIndex;

                IReadOnlyList<AnimDef> filtered = null;

                if (toolIdx >= 0 && config.toolBindings.Count > 0)
                {
                    var boundAnims = config.toolBindings
                        .Where(b => b.toolIndex == toolIdx)
                        .Select(b => b.animDefName)
                        .ToHashSet();

                    if (boundAnims.Count > 0)
                        filtered = __result.Where(a => boundAnims.Contains(a.defName)).ToList();
                }

                if (filtered == null && config.forcedAnimDefNames.Count > 0)
                {
                    var forcedSet = config.forcedAnimDefNames.ToHashSet();
                    filtered = __result.Where(a => forcedSet.Contains(a.defName)).ToList();
                }

                if (filtered != null && filtered.Count > 0)
                    __result = filtered;
            }
        }

        [HarmonyPatch(typeof(Dialog_TweakEditor), "DoWindowContents")]
        [HarmonyPostfix]
        static void AddAnimOverrideButton(Dialog_TweakEditor __instance, Rect inRect, ThingDef ___Def)
        {
            if (___Def == null) return;

            var rect = new Rect(inRect.x + inRect.width - 180, inRect.y + inRect.height - 40, 170, 30);

            if (Widgets.ButtonText(rect, "Edit Anim Overrides"))
                Find.WindowStack.Add(new Dialog_MerissuAnimConfig(___Def.defName));
        }
    }

    public class Dialog_MerissuAnimConfig : Window
    {
        private readonly string _defName;
        private MerissuAnimConfig _config;
        private Vector2 _scrollPos;
        private string _newAnimName = "";
        private string _newBindingAnim = "";
        private string _newBindingToolIdx = "0";
        private Vector2 _bindScrollPos;

        public override Vector2 InitialSize => new(500, 600);

        public Dialog_MerissuAnimConfig(string defName)
        {
            _defName = defName;
            _config = MerissuAnimOverrideManager.GetOrCreate(defName);
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(ls.GetRect(28), $"Anim Overrides: {_defName}");
            Text.Font = GameFont.Small;

            Widgets.Label(ls.GetRect(36),
                "Leave Forced Animations empty → use original logic.\n" +
                "Tool bindings force a specific animation when that tool is used.");

            ls.Gap(4);

            Widgets.Label(ls.GetRect(20), "Forced Animations (override):");
            ls.Gap(4);

            var listRect = ls.GetRect(140);
            var viewRect = new Rect(0, 0, listRect.width - 20, Mathf.Max(_config.forcedAnimDefNames.Count * 24 + 10, 30));
            Widgets.BeginScrollView(listRect, ref _scrollPos, viewRect);

            int toRemove = -1;
            for (int i = 0; i < _config.forcedAnimDefNames.Count; i++)
            {
                var row = new Rect(5, i * 24, viewRect.width - 10, 22);
                Widgets.Label(row.LeftPart(0.7f), _config.forcedAnimDefNames[i]);
                if (Widgets.ButtonText(row.RightPart(0.2f), "X", true, false, Color.red))
                    toRemove = i;
            }
            Widgets.EndScrollView();

            if (toRemove >= 0)
            {
                _config.forcedAnimDefNames.RemoveAt(toRemove);
                Messages.Message("Removed animation.", MessageTypeDefOf.PositiveEvent, false);
            }

            ls.Gap(2);
            var addRowRect = ls.GetRect(28);
            var selectBtnRect = addRowRect;
            selectBtnRect.width = 170;
            if (Widgets.ButtonText(selectBtnRect, "Select from available..."))
                OpenAnimSelectionMenu();

            var textRect = addRowRect;
            textRect.x = selectBtnRect.x + selectBtnRect.width + 5;
            textRect.width = addRowRect.width - selectBtnRect.width - 75;
            _newAnimName = Widgets.TextField(textRect, _newAnimName);

            var btnRect = addRowRect;
            btnRect.x = addRowRect.x + addRowRect.width - 65;
            btnRect.width = 60;
            if (GUI.Button(btnRect, "Add") && !_newAnimName.NullOrEmpty())
            {
                if (!_config.forcedAnimDefNames.Contains(_newAnimName))
                {
                    _config.forcedAnimDefNames.Add(_newAnimName);
                    Messages.Message($"Added: {_newAnimName}", MessageTypeDefOf.PositiveEvent, false);
                }
                else
                    Messages.Message($"Already in list: {_newAnimName}", MessageTypeDefOf.RejectInput, false);
                GUI.FocusControl(null);
                _newAnimName = "";
            }

            ls.Gap(4);
            if (ls.ButtonText("Show All Available Animations (for reference)"))
            {
                var allAnims = GetAvailableAnimations();
                string msg = $"Available animations for {_defName} (original logic):\n\n";
                foreach (var anim in allAnims.Take(40))
                    msg += $"[{anim.type}] {anim.defName} ({anim.Probability:P1})\n";
                if (allAnims.Count > 40)
                    msg += $"\n... and {allAnims.Count - 40} more.";
                Find.WindowStack.Add(new Dialog_MessageBox(msg));
            }

            ls.GapLine();
            ls.Gap(4);
            Widgets.Label(ls.GetRect(20), "Tool → Animation Bindings:");
            ls.Gap(4);

            var bindRect = ls.GetRect(130);
            var bindView = new Rect(0, 0, bindRect.width - 20, Mathf.Max(_config.toolBindings.Count * 28 + 10, 30));
            Widgets.BeginScrollView(bindRect, ref _bindScrollPos, bindView);

            int removeBinding = -1;
            for (int i = 0; i < _config.toolBindings.Count; i++)
            {
                var b = _config.toolBindings[i];
                var row = new Rect(5, i * 28, bindView.width - 10, 26);
                Widgets.Label(row.LeftPart(0.25f), $"Tool [{b.toolIndex}]");
                Widgets.Label(row.LeftPart(0.6f).RightPart(0.45f), $"→ {b.animDefName}");
                if (Widgets.ButtonText(row.RightPart(0.15f), "X", true, false, Color.red))
                    removeBinding = i;
            }
            Widgets.EndScrollView();

            if (removeBinding >= 0)
            {
                _config.toolBindings.RemoveAt(removeBinding);
                Messages.Message("Removed binding.", MessageTypeDefOf.PositiveEvent, false);
            }

            ls.Gap(2);
            var addBindRect = ls.GetRect(28);
            float curX = addBindRect.x, curY = addBindRect.y, curH = addBindRect.height;
            var toolBtnRect = new Rect(curX, curY, 120, curH);
            string toolBtnLabel = _newBindingToolIdx == "0" ? "Select Tool..." : $"Tool #{_newBindingToolIdx}";
            if (Widgets.ButtonText(toolBtnRect, toolBtnLabel))
                OpenToolSelectionMenu();
            curX += 125;

            Widgets.Label(new Rect(curX, curY, 20, curH), "→");
            curX += 22;

            var animBtnRect = new Rect(curX, curY, 180, curH);
            string animBtnLabel = string.IsNullOrEmpty(_newBindingAnim) ? "Select Anim(s)..." : $"Anims ({_newBindingAnim.Split(',').Length} selected)";
            if (Widgets.ButtonText(animBtnRect, animBtnLabel))
                OpenMultiAnimSelectionMenu();
            curX += 185;

            var addBindBtnRect = new Rect(curX, curY, 60, curH);
            if (GUI.Button(addBindBtnRect, "Add"))
            {
                if (int.TryParse(_newBindingToolIdx, out int idx) && !_newBindingAnim.NullOrEmpty())
                {
                    var animsToAdd = _newBindingAnim.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    foreach (var animName in animsToAdd)
                        _config.toolBindings.Add(new ToolAnimBinding { toolIndex = idx, animDefName = animName });
                    Messages.Message($"Added {animsToAdd.Count} anim(s) to Tool[{idx}]", MessageTypeDefOf.PositiveEvent, false);
                    _newBindingToolIdx = "0";
                    _newBindingAnim = "";
                    GUI.FocusControl(null);
                }
                else
                    Messages.Message("Invalid input: check tool index and select at least one animation.", MessageTypeDefOf.RejectInput, false);
            }

            ls.Gap(10);
            if (ls.ButtonText("Save to Config File"))
            {
                MerissuAnimOverrideManager.Save();
                Messages.Message($"[Merissu] Saved anim overrides for {_defName}", MessageTypeDefOf.PositiveEvent, false);
            }
            if (ls.ButtonText("Clear All Overrides for this Weapon"))
            {
                _config.forcedAnimDefNames.Clear();
                _config.toolBindings.Clear();
                Messages.Message("Cleared all overrides.", MessageTypeDefOf.PositiveEvent, false);
            }

            ls.End();
        }

        private void OpenMultiAnimSelectionMenu()
        {
            var allAnims = GetAvailableAnimations();
            if (allAnims.Count == 0)
            {
                Messages.Message("No available animations for this weapon.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var currentSelections = new HashSet<string>();
            if (!string.IsNullOrEmpty(_newBindingAnim))
                foreach (var s in _newBindingAnim.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    currentSelections.Add(s.Trim());

            var options = new List<FloatMenuOption>();
            foreach (var anim in allAnims.Take(50))
            {
                string defName = anim.defName;
                bool isSelected = currentSelections.Contains(defName);
                string color = isSelected ? "#97ff87" : "#ffffff";
                string checkMark = isSelected ? "✓ " : "  ";
                options.Add(new FloatMenuOption($"<color={color}>{checkMark}[{anim.type}] {defName} ({anim.Probability:P1})</color>", () =>
                {
                    if (currentSelections.Contains(defName))
                        currentSelections.Remove(defName);
                    else
                        currentSelections.Add(defName);
                    _newBindingAnim = string.Join(",", currentSelections);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public override void PostClose()
        {
            base.PostClose();
            MerissuAnimOverrideManager.Save();
        }

        private List<AnimDef> GetAvailableAnimations()
        {
            var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(_defName);
            if (weapon == null) return new List<AnimDef>();
            var input = new ReqInput(weapon);
            return AnimDef.AllDefs.Where(a => a.weaponFilter != null && a.weaponFilter.Evaluate(input))
                .OrderByDescending(a => a.Probability).ToList();
        }

        private void OpenAnimSelectionMenu(bool isForBinding = false)
        {
            var allAnims = GetAvailableAnimations();
            if (allAnims.Count == 0)
            {
                Messages.Message("No available animations for this weapon.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (var anim in allAnims.Take(50))
            {
                string defName = anim.defName;
                string label = $"[{anim.type}] {defName} ({anim.Probability:P1})";
                bool alreadyInForced = _config.forcedAnimDefNames.Contains(defName);
                string color = alreadyInForced ? "#97ff87" : "#ffffff";
                options.Add(new FloatMenuOption($"<color={color}>{label}</color>", () =>
                {
                    if (isForBinding) _newBindingAnim = defName;
                    else if (!_config.forcedAnimDefNames.Contains(defName))
                    {
                        _config.forcedAnimDefNames.Add(defName);
                        Messages.Message($"Added: {defName}", MessageTypeDefOf.PositiveEvent, false);
                    }
                    else Messages.Message($"Already in list: {defName}", MessageTypeDefOf.RejectInput, false);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenToolSelectionMenu()
        {
            var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(_defName);
            if (weapon == null)
            {
                Messages.Message("Weapon def not found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var tools = weapon.tools;
            if (tools == null || tools.Count == 0)
            {
                Messages.Message("This weapon has no tools defined.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var options = new List<FloatMenuOption>();
            for (int i = 0; i < tools.Count; i++)
            {
                int idx = i;
                var tool = tools[i];
                string label = $"Tool [{idx}]";
                if (!string.IsNullOrEmpty(tool.label)) label += $" - {tool.label}";
                if (tool.capacities != null && tool.capacities.Count > 0)
                    label += $" ({string.Join(", ", tool.capacities.Select(c => c.defName))})";
                options.Add(new FloatMenuOption(label, () => _newBindingToolIdx = idx.ToString()));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}