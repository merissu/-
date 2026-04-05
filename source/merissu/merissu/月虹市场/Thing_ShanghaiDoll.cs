using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Thing_ShanghaiDoll : Thing
    {
        private Pawn owner;
        private Vector3 lastPos;
        private bool faceRight;
        private Pawn targetEnemy;

        private const float SearchRadius = 10f;
        private const float AttackRadius = 1.5f; 
        private const float MoveSpeedPerTick = 8f / 60f; 
        private const float DamagePerHit = 4f;

        private const int IdleFrameCount = 4;
        private const int MoveFrameCount = 2;
        private const int AttackFrameCount = 4;
        private const int TicksPerFrame = 4;
        private const float DollScale = 0.65f;

        public void Init(Pawn owner)
        {
            this.owner = owner;
            this.lastPos = owner.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();
            if (owner == null || owner.Dead || !owner.Spawned) { this.Destroy(); return; }

            SearchForTarget();

            Vector3 logicalTarget;
            if (targetEnemy != null && targetEnemy.Spawned && !targetEnemy.Dead && !targetEnemy.Downed)
            {
                logicalTarget = targetEnemy.DrawPos;

                float distSqr = (this.lastPos - targetEnemy.DrawPos).sqrMagnitude;
                if (distSqr < AttackRadius * AttackRadius)
                {
                    if (Find.TickManager.TicksGame % 10 == 0) 
                    {
                        ApplyDamage(targetEnemy);
                    }
                }
            }
            else
            {
                logicalTarget = owner.DrawPos + new Vector3(-0.6f, 0, 0.6f);
                targetEnemy = null;
            }

            float dx = logicalTarget.x - lastPos.x;
            if (Mathf.Abs(dx) > 0.001f)
            {
                faceRight = dx < 0;
            }

            lastPos = Vector3.MoveTowards(lastPos, logicalTarget, MoveSpeedPerTick);
        }

        private void SearchForTarget()
        {
            if (targetEnemy != null && (targetEnemy.Dead || !targetEnemy.Spawned || targetEnemy.Downed || targetEnemy.Position.DistanceTo(owner.Position) > SearchRadius + 5f))
            {
                targetEnemy = null;
            }

            if (targetEnemy == null && Find.TickManager.TicksGame % 30 == 0)
            {
                targetEnemy = (Pawn)GenClosest.ClosestThingReachable(
                    owner.Position, owner.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                    PathEndMode.OnCell, TraverseParms.For(TraverseMode.NoPassClosedDoors),
                    SearchRadius,
                    x => x is Pawn p && p.HostileTo(owner) && !p.Downed); 
            }
        }

        private void ApplyDamage(Pawn victim)
        {
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, DamagePerHit, 0f, -1f, this);
            victim.TakeDamage(dinfo);
            if (Find.TickManager.TicksGame % 20 == 0) 
                SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(new TargetInfo(victim.Position, victim.Map));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            string path;
            int frameCount;

            float distToTarget = (targetEnemy != null) ? (lastPos - targetEnemy.DrawPos).magnitude : 999f;

            if (targetEnemy != null && distToTarget < AttackRadius)
            {
                path = "Accessory/doll/objectAe";
                frameCount = AttackFrameCount;
            }
            else if ((lastPos - (owner.DrawPos + new Vector3(-0.6f, 0, 0.6f))).magnitude > 0.1f || (targetEnemy != null && distToTarget >= AttackRadius))
            {
                path = "Accessory/doll/objectAb";
                frameCount = MoveFrameCount;
            }
            else
            {
                path = "Accessory/doll/objectAa";
                frameCount = IdleFrameCount;
            }

            int frameIndex = (Find.TickManager.TicksGame / TicksPerFrame) % frameCount;
            Material mat = MaterialPool.MatFrom($"{path}{frameIndex:D3}", ShaderDatabase.Cutout);

            float visualFloatY = (path == "Accessory/doll/objectAe") ? 0 : Mathf.Sin(Find.TickManager.TicksGame * 0.08f) * 0.1f;
            Vector3 renderPos = lastPos + new Vector3(0, 0, visualFloatY);
            renderPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            Mesh targetMesh = faceRight ? MeshPool.plane10Flip : MeshPool.plane10;
            Graphics.DrawMesh(targetMesh, Matrix4x4.TRS(renderPos, Quaternion.identity, new Vector3(DollScale, 1f, DollScale)), mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_References.Look(ref targetEnemy, "targetEnemy");
            Scribe_Values.Look(ref lastPos, "lastPos");
            Scribe_Values.Look(ref faceRight, "faceRight");
        }
    }
}