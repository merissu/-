using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Projectile_TrackKnife : Bullet
    {
        public int bounceCount = 5;
        public List<Pawn> hitTargets = new List<Pawn>();

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            Vector3 currentPos = this.DrawPos;
            Pawn launcherPawn = launcher as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (bounceCount > 0 && map != null && launcherPawn != null)
            {
                if (hitThing is Pawn p) hitTargets.Add(p);

                Pawn nextTarget = FindNextTarget(map, currentPos, launcherPawn);

                if (nextTarget != null)
                {
                    SpawnNextBounce(nextTarget, map, currentPos, launcherPawn);
                }
            }
        }

        private Pawn FindNextTarget(Map map, Vector3 origin, Pawn launcherPawn)
        {
            return map.mapPawns.AllPawnsSpawned
                .Where(p => p.HostileTo(launcherPawn) && !p.Downed && !p.Dead) 
                .Where(p => !hitTargets.Contains(p)) 
                .Where(p => p.Position.DistanceTo(origin.ToIntVec3()) <= 30f) 
                .Where(p => GenSight.LineOfSight(origin.ToIntVec3(), p.Position, map)) 
                .OrderBy(p => p.Position.DistanceToSquared(origin.ToIntVec3())) 
                .FirstOrDefault();
        }

        private void SpawnNextBounce(Pawn target, Map map, Vector3 origin, Pawn launcherPawn)
        {
            Projectile_TrackKnife nextBullet = (Projectile_TrackKnife)GenSpawn.Spawn(this.def, origin.ToIntVec3(), map);

            nextBullet.launcher = launcherPawn; 
            nextBullet.bounceCount = this.bounceCount - 1; 
            nextBullet.hitTargets = new List<Pawn>(this.hitTargets); 

            nextBullet.Launch(launcherPawn, origin, target, target, ProjectileHitFlags.All, false, null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref bounceCount, "bounceCount", 0);
            Scribe_Collections.Look(ref hitTargets, "hitTargets", LookMode.Reference);
        }
    }
}