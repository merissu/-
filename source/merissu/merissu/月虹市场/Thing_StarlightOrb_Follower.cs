using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using Verse.Sound; 
namespace merissu
{

    public class Thing_StarlightOrb_Follower : Thing
    {
        private Pawn owner;
        private Material laserMat;
        private Vector3 laserEnd;
        private float currentAngle;

        private const float FollowRadius = 1.5f;
        private const float CombatRadius = 2.5f;
        private const float RotateSpeedIdle = 2f;
        private const float LaserRange = 250f;
        private Sustainer sustainer; 
        public void Init(Pawn owner, Material mat)
        {
            this.owner = owner;
            this.laserMat = mat;
        }

        protected override void Tick()
        {
            if (owner == null || owner.Dead || !owner.Spawned)
            {
                EndSustainer(); 
                this.Destroy();
                return;
            }

            Pawn target = null;
            if (owner.Drafted)
            {
                target = FindBestTarget();
            }

            if (target != null)
            {
                Vector3 aimDir = (target.DrawPos - owner.DrawPos).normalized;
                float targetAngle = Mathf.Atan2(aimDir.z, aimDir.x) * Mathf.Rad2Deg;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.15f);

                UpdateLaser(target);

                StartOrMaintainSustainer();
            }
            else
            {
                float rimworldBackAngle = owner.Rotation.AsAngle + 180f;
                float targetAngle = 90f - rimworldBackAngle;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.05f);
                laserEnd = DrawPos;

                EndSustainer();
            }
        }

        private void StartOrMaintainSustainer()
        {
            if (sustainer == null || sustainer.Ended)
            {
                SoundDef soundDef = SoundDef.Named("se_lazer02");
                if (soundDef != null)
                {
                    sustainer = soundDef.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                }
            }

            sustainer?.Maintain();
        }

        private void EndSustainer()
        {
            if (sustainer != null)
            {
                sustainer.End(); 
                sustainer = null;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            EndSustainer();
            base.Destroy(mode);
        }
        private Pawn FindBestTarget()
        {
            return (Pawn)GenClosest.ClosestThingReachable(
                owner.Position,
                owner.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                Verse.AI.PathEndMode.OnCell,
                TraverseParms.For(owner),
                999f,
                x =>
                {
                    Pawn p = x as Pawn;
                    return p != null &&
                           p.HostileTo(owner) &&
                           !p.Dead &&
                           !p.Downed &&
                           !p.IsPrisoner && 
                           GenSight.LineOfSight(owner.Position, p.Position, owner.Map);
                });
        }
        private void UpdateLaser(Pawn target)
        {
            Vector3 start = DrawPos;
            Vector3 dir = (target.DrawPos - start).normalized;
            laserEnd = start + dir * LaserRange;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start.ToIntVec3(), laserEnd.ToIntVec3()))
            {
                if (!cell.InBounds(Map)) break;
                Building b = cell.GetFirstBuilding(Map);
                if (b != null && b.def.Fillage == FillCategory.Full)
                {
                    laserEnd = b.DrawPos;
                    break;
                }

                if (Find.TickManager.TicksGame % 6 == 0) 
                {
                    var victim = cell.GetFirstPawn(Map);
                    if (victim != null && victim.HostileTo(owner))
                    {
                        victim.TakeDamage(new DamageInfo(DamageDefOf.Flame, 3f, 0, -1, owner));
                    }
                }
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (owner == null) return base.DrawPos;

                float rad = currentAngle * Mathf.Deg2Rad;
                float dist = owner.Drafted ? CombatRadius : FollowRadius;

                float xOffset = Mathf.Cos(rad) * dist;
                float zOffset = Mathf.Sin(rad) * dist;

                float floatingZ = zOffset + Mathf.Sin(Find.TickManager.TicksGame * 0.05f) * 0.15f;

                Vector3 offset = new Vector3(xOffset, 0.5f, floatingZ);

                return owner.DrawPos + offset;
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (owner != null && owner.Drafted && laserMat != null)
            {
                StarlightLaserDrawer.DrawLaser(drawLoc, laserEnd, laserMat, 1.5f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref currentAngle, "currentAngle");
        }
    }
}