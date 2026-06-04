using RimWorld;
using Verse;

namespace merissu
{
    [RimWorld.DefOf]
    public static class STGKeyDefOf
    {
        public static KeyBindingDef Merissu_MoveUp;
        public static KeyBindingDef Merissu_MoveDown;
        public static KeyBindingDef Merissu_MoveLeft;
        public static KeyBindingDef Merissu_MoveRight;
        public static KeyBindingDef PS_Sneak;
        public static KeyBindingDef ToggleManualControl;
        public static KeyBindingDef ToggleCameraZoom;

        static STGKeyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(STGKeyDefOf));
        }
    }
}