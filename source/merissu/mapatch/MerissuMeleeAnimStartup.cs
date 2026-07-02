using HarmonyLib;
using Verse;

[StaticConstructorOnStartup]
public static class MerissuMeleeAnimStartup
{
    static MerissuMeleeAnimStartup()
    {
        new Harmony("merissu.meleeanimoverride").PatchAll();
    }
}
