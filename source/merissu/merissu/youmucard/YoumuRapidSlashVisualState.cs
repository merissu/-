using System.Collections.Generic;
using Verse;

namespace merissu
{
    public static class YoumuRapidSlashVisualState
    {
        private static readonly HashSet<int> ActivePawnIds = new HashSet<int>();

        public static void SetActive(Pawn pawn, bool active)
        {
            if (pawn == null) return;

            int id = pawn.thingIDNumber;
            if (active) ActivePawnIds.Add(id);
            else ActivePawnIds.Remove(id);
        }

        public static bool IsActive(Pawn pawn)
        {
            if (pawn == null) return false;
            return ActivePawnIds.Contains(pawn.thingIDNumber);
        }
        public static void ClearAll()
        {
            ActivePawnIds.Clear();
        }
    }
}