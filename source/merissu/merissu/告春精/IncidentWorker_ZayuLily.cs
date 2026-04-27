using RimWorld;
using Verse;

namespace merissu
{
    public class IncidentWorker_ZayuLily : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;

            if (GenLocalDate.Season(map) != Season.Spring) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            IntVec3 loc;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out loc, map, CellFinder.EdgeRoadChance_Animal))
            {
                return false;
            }

            PawnKindDef lilyKind = PawnKindDef.Named("ZayuLilyKind");
            Pawn lily = PawnGenerator.GeneratePawn(lilyKind, null);

            GenSpawn.Spawn(lily, loc, map, Rot4.Random);

            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, lily);

            return true;
        }
    }
}