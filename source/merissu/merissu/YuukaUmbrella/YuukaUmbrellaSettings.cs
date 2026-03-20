using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class YuukaUmbrellaSettings : ModSettings
    {
        public static Dictionary<string, Vector2> offsets = new Dictionary<string, Vector2>
        {
            { "North", new Vector2(-0.01f, 0.11f) },
            { "South", new Vector2(0.01f, 0.14f) },
            { "East",  new Vector2(-0.03f, 0.14f) },
            { "West",  new Vector2(0.03f, 0.13f) }
        };

        public static Dictionary<string, float> angles = new Dictionary<string, float>
        {
            { "North", -27f }, { "South", 20f }, { "East", -30f }, { "West", 23f }
        };

        public static Dictionary<string, float> yOffsets = new Dictionary<string, float>
        {
            { "North", 0.020f },
            { "South", -0.065f },
            { "East",  -0.071f },
            { "West",  -0.020f }
        };

        public override void ExposeData()
        {
            base.ExposeData();
            foreach (var dir in new[] { "North", "South", "East", "West" })
            {
                Vector2 pos = offsets[dir];
                float ang = angles[dir];
                float yOff = yOffsets[dir];

                Scribe_Values.Look(ref pos, "offset" + dir, offsets[dir]);
                Scribe_Values.Look(ref ang, "angle" + dir, angles[dir]);
                Scribe_Values.Look(ref yOff, "yOffset" + dir, yOffsets[dir]);

                offsets[dir] = pos;
                angles[dir] = ang;
                yOffsets[dir] = yOff;
            }
        }
    }

    public class YuukaUmbrellaMod : Mod
    {
        private Vector2 scrollPosition = Vector2.zero;

        public YuukaUmbrellaMod(ModContentPack content) : base(content)
        {
            GetSettings<YuukaUmbrellaSettings>();
            var harmony = new Harmony("merissu.yuukaumbrella.livepatch");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, 1000f);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("<size=18><b>风见幽香的阳伞 - 装备位置调整</b></size>");
            listing.GapLine();

            string[] directions = new[] { "North", "South", "East", "West" };
            foreach (var dir in directions)
            {
                listing.Label($"方向: <color=yellow><b>{dir}</b></color>");

                Vector2 currentPos = YuukaUmbrellaSettings.offsets[dir];
                listing.Label($"  左右偏移 (X): {currentPos.x:F2}");
                currentPos.x = listing.Slider(currentPos.x, -1.5f, 1.5f);

                listing.Label($"  高度偏移 (Z): {currentPos.y:F2}");
                currentPos.y = listing.Slider(currentPos.y, -1.5f, 1.5f);
                YuukaUmbrellaSettings.offsets[dir] = currentPos;

                float y = YuukaUmbrellaSettings.yOffsets[dir];
                listing.Label($"  层级偏移 (Y/前后): {y:F3} (正值在前, 负值在后)");
                YuukaUmbrellaSettings.yOffsets[dir] = listing.Slider(y, -0.2f, 0.2f);

                float ang = YuukaUmbrellaSettings.angles[dir];
                listing.Label($"  旋转角度: {ang:F0}°");
                YuukaUmbrellaSettings.angles[dir] = listing.Slider(ang, -180f, 180f);

                listing.Gap(15f);
                listing.GapLine();
            }

            if (listing.ButtonText("重置为默认值"))
            {
                ResetSettings();
            }

            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        private void ResetSettings()
        {
            YuukaUmbrellaSettings.offsets["North"] = new Vector2(-0.01f, 0.11f);
            YuukaUmbrellaSettings.offsets["South"] = new Vector2(0.01f, 0.14f);
            YuukaUmbrellaSettings.offsets["East"] = new Vector2(-0.03f, 0.14f);
            YuukaUmbrellaSettings.offsets["West"] = new Vector2(0.03f, 0.13f);

            YuukaUmbrellaSettings.yOffsets["North"] = 0.020f;
            YuukaUmbrellaSettings.yOffsets["South"] = -0.065f;
            YuukaUmbrellaSettings.yOffsets["East"] = -0.071f;
            YuukaUmbrellaSettings.yOffsets["West"] = -0.020f;

            YuukaUmbrellaSettings.angles["North"] = -27f;
            YuukaUmbrellaSettings.angles["South"] = 20f;
            YuukaUmbrellaSettings.angles["East"] = -30f;
            YuukaUmbrellaSettings.angles["West"] = 23f;
        }

        public override string SettingsCategory() => "花田阳伞设置";
    }

    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_YuukaUmbrellaRender
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            if (eq?.def?.defName == "OpenYuukaUmbrella")
            {
                Pawn pawn = (eq.ParentHolder as Pawn_EquipmentTracker)?.pawn;
                if (pawn != null)
                {
                    Rot4 rot = pawn.Rotation;
                    string dirKey = rot == Rot4.North ? "North" :
                                    rot == Rot4.South ? "South" :
                                    rot == Rot4.East ? "East" : "West";

                    if (YuukaUmbrellaSettings.offsets.TryGetValue(dirKey, out Vector2 offset))
                    {
                        drawLoc.x += offset.x;
                        drawLoc.z += offset.y;
                    }

                    if (YuukaUmbrellaSettings.yOffsets.TryGetValue(dirKey, out float yOffset))
                    {
                        drawLoc.y += yOffset;
                    }

                    if (YuukaUmbrellaSettings.angles.TryGetValue(dirKey, out float angle))
                    {
                        aimAngle = angle;
                    }
                }
            }
            return true;
        }
    }
}