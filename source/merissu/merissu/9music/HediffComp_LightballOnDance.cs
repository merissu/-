using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffComp_LightballOnDance : HediffComp
    {
        private Mote rotationMote;
        private Mote lightsMote;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent.pawn;

            if (pawn == null || pawn.Map == null || pawn.Dead)
            {
                DestroyMotes();
                return;
            }

            if (pawn.CurJobDef != RimWorld.JobDefOf.Dance)
            {
                DestroyMotes();
                return;
            }

            Vector3 pos = pawn.DrawPos;

            if (rotationMote == null || rotationMote.Destroyed)
            {
                rotationMote = MoteMaker.MakeStaticMote(
                    pos,
                    pawn.Map,
                    ThingDefOf.Mote_LightBall
                );
            }

            rotationMote.exactPosition = pos;
            rotationMote.Maintain();

            if (lightsMote == null || lightsMote.Destroyed)
            {
                lightsMote = MoteMaker.MakeStaticMote(
                    pos,
                    pawn.Map,
                    ThingDefOf.Mote_LightBallLights
                );

                if (lightsMote != null)
                {
                    lightsMote.rotationRate = -3f;
                }
            }

            lightsMote.exactPosition = pos;
            lightsMote.Maintain();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            DestroyMotes();
        }

        private void DestroyMotes()
        {
            rotationMote?.Destroy();
            rotationMote = null;

            lightsMote?.Destroy();
            lightsMote = null;
        }
    }
}
