using Verse;
using RimWorld;
using UnityEngine;

namespace merissu
{
    public class Verb_ShootStarlightLaser : Verb
    {
        protected override bool TryCastShot()
        {
            Pawn pawn = CasterPawn;
            if (pawn == null || pawn.Map == null)
                return false;

            Vector3 target = currentTarget.CenterVector3;

            Vector3 dir = (target - pawn.DrawPos).normalized;

            Vector3 start = pawn.DrawPos + dir * 1.0f;

            Thing_StarlightLaserBeam beam =
                (Thing_StarlightLaserBeam)ThingMaker.MakeThing(
                    DefDatabase<ThingDef>.GetNamed("StarlightLaserBeam")
                );

            beam.Init(start, dir, pawn);

            GenSpawn.Spawn(
                beam,
                start.ToIntVec3(),
                pawn.Map
            );

            return true;
        }
    }
}
