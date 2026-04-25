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
       public static DutyDef Merissu_AssaultMaoyu;  
      //  public static DutyDef Merissu_AssaultTypeB;  //指挥官B

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
        private static readonly HashSet<string> TypeAKinds = new HashSet<string>
        {
            "ZayuMaoYuKind",//a种族列表
            "ZayuMaoYuKindB",
            "ZayuMaoYuKindC",
            "ZayuMaoYuKindD",
            "ZayuMaoYuKindE",
        };

        private static readonly HashSet<string> TypeBKinds = new HashSet<string>
        {
            "Merissu_WarFlyerKind", //b种族列表
        };

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn p = lord.ownedPawns[i];
                if (p == null || p.mindState == null) continue;

                DutyDef duty = MerissuDutyDefOf.Merissu_AssaultRanged; 
                string kindDefName = p.kindDef?.defName;

                if (!kindDefName.NullOrEmpty())
                {
                    if (TypeAKinds.Contains(kindDefName))
                        duty = MerissuDutyDefOf.Merissu_AssaultMaoyu;//如果是a列表就切指挥官a
                    else if (TypeBKinds.Contains(kindDefName))
                        duty = MerissuDutyDefOf.Merissu_AssaultRanged;//如果是b列表就切指挥官b
                }

                p.mindState.duty = new PawnDuty(duty);
            }
        }
    }
}