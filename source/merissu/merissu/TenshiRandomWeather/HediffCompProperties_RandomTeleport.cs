using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace merissu
{
    public class HediffCompProperties_KawamuraRandomMove : HediffCompProperties
    {
        public int intervalTicks = 300; 
        public float radius = 5f;     

        public HediffCompProperties_KawamuraRandomMove()
        {
            this.compClass = typeof(HediffComp_KawamuraRandomMove);
        }
    }

    public class HediffComp_KawamuraRandomMove : HediffComp
    {
        public HediffCompProperties_KawamuraRandomMove Props => (HediffCompProperties_KawamuraRandomMove)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.Spawned && Pawn.Map != null && Pawn.IsHashIntervalTick(Props.intervalTicks))
            {
                DoKawamuraMove();
            }
        }

        private void DoKawamuraMove()
        {
            Map map = Pawn.Map;
            IntVec3 currentPos = Pawn.Position;

            int numCells = GenRadial.NumCellsInRadius(Props.radius);
            IntVec3 randomOffset = GenRadial.RadialPattern[Rand.Range(0, numCells)];
            IntVec3 targetPos = currentPos + randomOffset;

            if (targetPos.InBounds(map) && targetPos.Walkable(map) && targetPos != currentPos)
            {
                Pawn.Position = targetPos;

                Pawn.pather?.ResetToCurrentPosition();
            }
        }
    }
}