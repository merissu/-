using HarmonyLib;
using System.Reflection;
using Verse;

namespace merissu
{
    public class MyMod : Mod
    {
        public MyMod(ModContentPack content) : base(content)
        {
            Log.Message("少女祈祷中...");
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyEntry
    {
        static HarmonyEntry()
        {
            new Harmony("touhou.merissu").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}