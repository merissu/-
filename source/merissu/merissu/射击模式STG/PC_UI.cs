using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public partial class PC
    {

        public void RenderPawn()
        {
            if (pawn.Map == null || !pawn.Spawned) return;

            LeanSmoothed = Vector3.SmoothDamp(LeanSmoothed, LeanTarget, ref _leanVelocity, 0.07f, 10f, Time.deltaTime);

            if (pawn.Drawer?.leaner != null)
            {
                IntVec3 snapped = IntVec3.Zero;
                if (LeanTarget.sqrMagnitude > 0.01f)
                {
                    snapped = Mathf.Abs(LeanTarget.x) >= Mathf.Abs(LeanTarget.z)
                        ? (LeanTarget.x > 0 ? IntVec3.East : new IntVec3(-1, 0, 0))
                        : (LeanTarget.z > 0 ? IntVec3.North : IntVec3.South);
                }
                pawn.Drawer.leaner.shootSourceOffset = snapped;
            }

            if (!physicsPosition.HasValue) return;
            var tweener = pawn.Drawer.tweener;
            tweener.lastTickSpringPos = tweener.tweenedPos;
            tweener.tweenedPos = physicsPosition.Value;
            tweener.lastDrawFrame = RealTime.frameCount;
            tweener.lastDrawTick = GenTicks.TicksGame;
        }
    }
}
