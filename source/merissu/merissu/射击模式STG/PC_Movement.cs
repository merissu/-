using merissu;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Windows;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace merissu
{
    public partial class PC
    {
        private float moveInputDuration = 0f;
        private bool wasSprinting = false;
        private IntVec3 prevCell = IntVec3.Invalid;
        private Vector3 _leanVelocity = Vector3.zero;
        private const float jobInterruptDelay = 0.35f;
        private const float maxPhysicsDesyncDistSq = 2.25f;
        public void UpdatePhysics()
        {
            if (pawn.Map == null || pawn.Map != Find.CurrentMap)
            {
                moveInput = Vector3.zero;
                physicsPosition = null;
                wasMovingLastFrame = false;
                return;
            }

            if (pawn.Downed)
            {
                physicsPosition = null;
                wasMovingLastFrame = false;
                return;
            }

            if (!pawn.Spawned || pawn.Map == null)
            {
                physicsPosition = null;
                wasMovingLastFrame = false;
                return;
            }

            if (pawn.InMentalState)
            {
                moveInput = Vector3.zero;
                if (wasMovingLastFrame)
                {
                    if (pawn.pather?.curPath != null)
                        pawn.pather.StopDead();
                    wasMovingLastFrame = false;
                }
                physicsPosition = null;
                return;
            }


            bool inCombatStance =
                pawn.stances.curStance is Stance_Warmup ||
                pawn.stances.curStance is Stance_Cooldown;

            if (State.ControlsFrozen ||
                State.CameraLockPosition.HasValue)
            {
                moveInput = Vector3.zero;
            }
            else
            {
                UpdateInput();
            }

            if (moveInput == Vector3.zero &&
                !pawn.Position.Walkable(pawn.Map))
            {
                IntVec3 best = IntVec3.Invalid;

                foreach (var adj in GenAdj.AdjacentCells)
                {
                    IntVec3 c = pawn.Position + adj;

                    if (c.InBounds(pawn.Map) && c.Walkable(pawn.Map))
                    {
                        best = c;
                        break;
                    }
                }

                if (best.IsValid)
                {
                    Vector3 dir =
                        (best.ToVector3Shifted() -
                         pawn.Position.ToVector3Shifted()).normalized;

                    moveInput = dir;
                }
            }

            ProcessMovement();
        }
        public void UpdateCamera()
        {
            if (pawn == null) return;

            var driver = Find.CameraDriver;
            if (driver == null) return;

            var sizeRange = new FloatRange(1.5f, 60f);

            Vector3 targetCamPos = Vector3.zero;

            if (pawn.Map == Find.CurrentMap && pawn.Spawned)
            {
                targetCamPos =
                    State.CameraLockPosition
                    ?? physicsPosition
                    ?? pawn.Position.ToVector3ShiftedWithAltitude(pawn.def.Altitude);
            }
            else
            {
                var container = State.TryGetSpawnedContainer(pawn);
                if (container != null && container.Map == Find.CurrentMap)
                    targetCamPos = container.DrawPos;
                else
                    return;
            }

            Vector3 newPos = targetCamPos;
            driver.rootPos = newPos;

            var cam = driver.GetComponent<Camera>();
            if (cam != null)
            {
                Vector3 finalPos = newPos;

                float rangeSpan = sizeRange.max - sizeRange.min;
                if (rangeSpan <= 0.01f) rangeSpan = 0.01f;

                finalPos.y = 15f + (driver.RootSize - sizeRange.min) / rangeSpan * 50f;

                cam.transform.position = finalPos + driver.shaker.ShakeOffset;
                cam.orthographicSize = driver.RootSize;
            }
        }
        private void UpdateInput()
        {
            moveInput = Vector3.zero;
            if (WorldRendererUtility.WorldSelected) return;

            if (STGKeyDefOf.Merissu_MoveUp.IsDown) moveInput += Vector3.forward;
            if (STGKeyDefOf.Merissu_MoveDown.IsDown) moveInput += Vector3.back;
            if (STGKeyDefOf.Merissu_MoveLeft.IsDown) moveInput += Vector3.left;
            if (STGKeyDefOf.Merissu_MoveRight.IsDown) moveInput += Vector3.right;

            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            isSneaking = STGKeyDefOf.PS_Sneak.IsDown;
        }
        private void ProcessMovement()
        {
            if (!pawn.Drafted && pawn.IsUnderAIControl())
            {
                physicsPosition = null;
                return;
            }

            if (!IsMoving)
            {
                moveInputDuration = 0f;
                if (wasMovingLastFrame)
                {
                    if (pawn.pather.curPath != null) pawn.pather.StopDead();
                    wasMovingLastFrame = false;
                }

                if (pawn.pather.curPath != null)
                {
                    physicsPosition = null;
                }

                if (physicsPosition.HasValue && physicsPosition.Value.ToIntVec3() != pawn.Position)
                {
                    physicsPosition = null;
                }
                return;
            }

            moveInputDuration += Time.deltaTime;

            if (!wasMovingLastFrame && physicsPosition.HasValue)
            {
                if (physicsPosition.Value.ToIntVec3() != pawn.Position)
                {
                    State.Warning($"位置不同步：当前={physicsPosition.Value.ToIntVec3()} | 实际={pawn.Position} (已重置)");
                    physicsPosition = null;
                }
            }

            if (!physicsPosition.HasValue)
            {
                physicsPosition = pawn.Position.ToVector3ShiftedWithAltitude(pawn.def.Altitude);
            }

            float distSq = (physicsPosition.Value.ToIntVec3() - pawn.Position).LengthHorizontalSquared;
            if (distSq > maxPhysicsDesyncDistSq)
            {
                physicsPosition = pawn.Position.ToVector3ShiftedWithAltitude(pawn.def.Altitude);
            }

            if (!wasMovingLastFrame)
            {
                wasMovingLastFrame = true;
            }

            if (pawn.jobs?.curJob != null && pawn.jobs.curJob.def.playerInterruptible)
            {
                bool isOurWaitJob =
                    (pawn.jobs.curJob.def == RimWorld.JobDefOf.Wait &&
                     pawn.jobs.curJob.expiryInterval == 60)
                    || pawn.jobs.curJob.def == RimWorld.JobDefOf.Wait_Combat;

                if (!isOurWaitJob && moveInputDuration > jobInterruptDelay)
                {
                    bool isShooting =
                        pawn.stances.curStance is Stance_Warmup ||
                        pawn.stances.curStance is Stance_Cooldown;

                    if (isShooting)
                        return;
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }

            Vector3 deltaRaw = moveInput.normalized;

            float speed;

            if (isSneaking)
            {
                speed = 4.6f * Time.deltaTime * Find.TickManager.TickRateMultiplier;
            }
            else
            {
                float baseSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
                speed = baseSpeed * 0.7f * Time.deltaTime * Find.TickManager.TickRateMultiplier;
            }
            float maxSpeed = 4.6f * 5f * Time.deltaTime * Find.TickManager.TickRateMultiplier;

            speed = Mathf.Min(speed, maxSpeed);
            Vector3 newPos = physicsPosition.Value;
            float distanceRemaining = speed;

            while (distanceRemaining > 0)
            {
                var step = Mathf.Min(distanceRemaining, 0.05f);
                distanceRemaining -= step;

                Vector3 stepDelta = deltaRaw * step;
                var currentlyWalkable = IsWalkableWithMargin(newPos);
                var safePos = pawn.Position.ToVector3Shifted();

                if (Mathf.Abs(stepDelta.x) > 0.0001f)
                {
                    Vector3 testX = newPos + new Vector3(stepDelta.x, 0, 0);
                    if (IsWalkableWithMargin(testX)) newPos = testX;
                    else if (!currentlyWalkable)
                    {
                        if ((testX - safePos).sqrMagnitude < (newPos - safePos).sqrMagnitude) newPos = testX;
                    }
                }

                if (Mathf.Abs(stepDelta.z) > 0.0001f)
                {
                    Vector3 testZ = newPos + new Vector3(0, 0, stepDelta.z);
                    if (IsWalkableWithMargin(testZ)) newPos = testZ;
                    else if (!currentlyWalkable)
                    {
                        if ((testZ - safePos).sqrMagnitude < (newPos - safePos).sqrMagnitude) newPos = testZ;
                    }
                }
            }

            var nextCell = newPos.ToIntVec3();

            if (nextCell.InBounds(pawn.Map) && nextCell.OnEdge(pawn.Map) && pawn.Map.exitMapGrid.IsExitCell(nextCell) && !pawn.Position.OnEdge(pawn.Map))
            {
                pawn.ExitMap(true, pawn.Rotation);
                return;
            }

            physicsPosition = newPos;

            if (pawn.Position != nextCell)
            {
                prevCell = pawn.Position;
                pawn.Position = nextCell;

                pawn.Notify_Teleported(
                    endCurrentJob: false,
                    resetTweenedPos: false);

                pawn.pather.nextCell = nextCell;
            }

            if (pawn.Drawer?.leaner != null && !(pawn.stances.curStance is Stance_Busy))
            {
                LeanTarget = Vector3.zero;
            }

            if (!(pawn.stances.curStance is Stance_Busy))
            {
                bool doingJob =
                    pawn.jobs?.curJob != null &&
                    pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait &&
                    pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait_Combat;

                if (!doingJob)
                {
                    UpdateRotation(moveInput.normalized);
                }
            }
        }
        private void UpdateRotation(Vector3 dir)
        {
            if (dir.x < -0.1f && dir.z > 0.1f) pawn.Rotation = Rot4.West;
            else if (dir.x > 0.1f && dir.z < -0.1f) pawn.Rotation = Rot4.East;
            else if (dir.x > 0.1f && dir.z > 0.1f) pawn.Rotation = Rot4.East;
            else if (dir.x < -0.1f && dir.z < -0.1f) pawn.Rotation = Rot4.West;
            else
            {
                var angle = NormAngle(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                pawn.Rotation = Rot4.FromAngleFlat(angle);
            }
        }

        private static float NormAngle(float a)
        {
            while (a < 0f) a += 360f;
            while (a >= 360f) a -= 360f;
            return a;
        }


        private bool IsWalkableWithMargin(Vector3 pos)
        {
            float margin = 0.15f;
            var cCenter = pos.ToIntVec3();

            if (!IsWalkableCell(cCenter)) return false;

            var c1 = new Vector3(pos.x + margin, pos.y, pos.z + margin).ToIntVec3();
            if (c1 != cCenter && !IsWalkableCell(c1)) return false;

            var c2 = new Vector3(pos.x - margin, pos.y, pos.z + margin).ToIntVec3();
            if (c2 != cCenter && !IsWalkableCell(c2)) return false;

            var c3 = new Vector3(pos.x + margin, pos.y, pos.z - margin).ToIntVec3();
            if (c3 != cCenter && !IsWalkableCell(c3)) return false;

            var c4 = new Vector3(pos.x - margin, pos.y, pos.z - margin).ToIntVec3();
            if (c4 != cCenter && !IsWalkableCell(c4)) return false;

            return true;
        }

        private bool IsWalkableCell(IntVec3 cell)
        {
            if (!cell.InBounds(pawn.Map)) return false;

            if (cell.GetDoor(pawn.Map) != null) return true;

            if (!cell.WalkableBy(pawn.Map, pawn)) return false;

            return true;
        }
        private void UpdateVehiclePhysics(Pawn vehicle, bool isDriver)
        {
            if (isDriver)
            {
                return;
            }
        }
    }
}
