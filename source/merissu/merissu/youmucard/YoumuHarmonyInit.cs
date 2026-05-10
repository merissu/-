using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class YoumuHarmonyInit
    {
        static YoumuHarmonyInit()
        {
            var harmony = new Harmony("merissu.youmu.rapidslash.tweener");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(PawnTweener), "PreDrawPosCalculation")]
    public static class Patch_PawnTweener_PreDrawPosCalculation
    {
        private const float VanillaSpring = 0.09f;
        private const float RapidSlashSpring = 0.35f;

        private static readonly AccessTools.FieldRef<PawnTweener, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnTweener, Pawn>("pawn");

        private static float GetSpringForTweener(PawnTweener tweener)
        {
            Pawn pawn = null;
            try
            {
                pawn = PawnRef(tweener);
            }
            catch
            {
                return VanillaSpring;
            }

            return YoumuRapidSlashVisualState.IsActive(pawn) ? RapidSlashSpring : VanillaSpring;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var list = new List<CodeInstruction>(instructions);
            var getSpringMI = AccessTools.Method(typeof(Patch_PawnTweener_PreDrawPosCalculation), nameof(GetSpringForTweener));

            bool replaced = false;

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];

                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float f && f == VanillaSpring)
                {
                    list[i] = new CodeInstruction(OpCodes.Ldarg_0); 
                    list.Insert(i + 1, new CodeInstruction(OpCodes.Call, getSpringMI));
                    replaced = true;
                    i++; 
                }
            }

            if (!replaced)
            {
                Log.Warning("[merissu.youmu.rapidslash.tweener] Transpiler did not find VanillaSpring constant in PawnTweener.PreDrawPosCalculation. Game update or conflict may have changed IL.");
            }

            return list;
        }
    }
}