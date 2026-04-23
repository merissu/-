using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace merissu
{
    [RimWorld.DefOf]
    public static class MerissuDutyDefOf
    {
        public static DutyDef Merissu_AssaultRanged;

        static MerissuDutyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MerissuDutyDefOf));
        }
    }

    public class LordJob_MerissuAssault : LordJob
    {
        public override bool CanBlockHostileVisitors => true;

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_MerissuAssault assault = new LordToil_MerissuAssault();
            graph.AddToil(assault);

            graph.StartingToil = assault;

            return graph;
        }
    }

    public class LordToil_MerissuAssault : LordToil
    {
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn p = lord.ownedPawns[i];
                if (p.mindState == null) continue;

                p.mindState.duty = new PawnDuty(MerissuDutyDefOf.Merissu_AssaultRanged);
            }
        }
    }
}