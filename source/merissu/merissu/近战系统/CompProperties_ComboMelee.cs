using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace merissu
{
    public class ComboSequence
    {
        public List<string> attacks = new List<string>();
    }

    public class CompProperties_ComboMelee : CompProperties
    {
        public List<ComboSequence> comboList = new List<ComboSequence>();
        public int resetTicks = 120;

        public CompProperties_ComboMelee()
        {
            this.compClass = typeof(CompComboMelee);
        }
    }

    public class CompComboMelee : ThingComp
    {
        public CompProperties_ComboMelee Props => (CompProperties_ComboMelee)props;

        private int currentGroupIndex = 0;
        private int currentAttackIndex = 0;
        private int lastAttackTick = -9999;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentGroupIndex, "currentGroupIndex", 0);
            Scribe_Values.Look(ref currentAttackIndex, "currentAttackIndex", 0);
            Scribe_Values.Look(ref lastAttackTick, "lastAttackTick", -9999);
        }

        public void CheckReset()
        {
            if (Find.TickManager.TicksGame - lastAttackTick > Props.resetTicks || lastAttackTick == -9999)
            {
                ResetAndPickNewCombo();
            }
        }

        public void ResetAndPickNewCombo()
        {
            currentAttackIndex = 0;
            if (!Props.comboList.NullOrEmpty())
            {
                currentGroupIndex = Rand.Range(0, Props.comboList.Count);
            }
        }

        public void AdvanceCombo()
        {
            currentAttackIndex++;
            var currentSequence = Props.comboList[currentGroupIndex].attacks;

            if (currentAttackIndex >= currentSequence.Count)
            {
                ResetAndPickNewCombo();
            }
            lastAttackTick = Find.TickManager.TicksGame;
        }

        public string GetExpectedToolLabel()
        {
            CheckReset();

            if (Props.comboList.NullOrEmpty()) return null;
            var currentSequence = Props.comboList[currentGroupIndex].attacks;

            if (currentSequence.NullOrEmpty()) return null;
            return currentSequence[currentAttackIndex];
        }
    }

    [StaticConstructorOnStartup]
    public static class ComboMeleePatches
    {
        static ComboMeleePatches()
        {
            var harmony = new Harmony("merissu.combomelee");
            harmony.PatchAll();

            Type ceVerbType = AccessTools.TypeByName("CombatExtended.Verb_MeleeAttackCE");
            if (ceVerbType != null)
            {
                MethodInfo ceTryCastShot = AccessTools.Method(ceVerbType, "TryCastShot");
                if (ceTryCastShot != null)
                {
                    MethodInfo postfix = AccessTools.Method(typeof(ComboMeleePatches), "CETryCastShotPostfix");
                    if (postfix != null)
                    {
                        harmony.Patch(ceTryCastShot, postfix: new HarmonyMethod(postfix));
                    }
                }
            }
        }

        public static void CETryCastShotPostfix(Verb __instance, bool __result)
        {
            Patch_TryCastShot_Impl(__instance as Verb_MeleeAttack, __result);
        }

        internal static void Patch_TryCastShot_Impl(Verb_MeleeAttack __instance, bool __result)
        {
            if (!__result) return;

            ThingWithComps weapon = __instance.EquipmentSource;
            if (weapon != null)
            {
                CompComboMelee comboComp = weapon.GetComp<CompComboMelee>();
                if (comboComp != null && __instance.tool != null)
                {
                    if (__instance.tool.label == comboComp.GetExpectedToolLabel())
                    {
                        comboComp.AdvanceCombo();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_TryCastShot
    {
        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            ComboMeleePatches.Patch_TryCastShot_Impl(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryGetMeleeVerb")]
    public static class Patch_TryGetMeleeVerb
    {
        public static void Postfix(Pawn_MeleeVerbs __instance, ref Verb __result, Thing target)
        {
            Pawn pawn = __instance.Pawn;
            if (pawn == null || pawn.equipment == null || pawn.equipment.Primary == null) return;

            ThingWithComps weapon = pawn.equipment.Primary;
            CompComboMelee comboComp = weapon.GetComp<CompComboMelee>();

            if (comboComp != null)
            {
                string expectedLabel = comboComp.GetExpectedToolLabel();
                if (expectedLabel == null) return;

                CompEquippable eqComp = weapon.GetComp<CompEquippable>();
                if (eqComp != null)
                {
                    foreach (Verb verb in eqComp.AllVerbs)
                    {
                        if (verb.tool != null && verb.tool.label == expectedLabel && verb.IsMeleeAttack)
                        {
                            __result = verb;
                            return;
                        }
                    }
                }
            }
        }
    }
}
