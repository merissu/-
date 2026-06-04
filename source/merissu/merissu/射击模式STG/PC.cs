using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace merissu
{
    [StaticConstructorOnStartup]
    public partial class PC : IExposable
    {
        public Pawn pawn;
        public Lord savedLord;
        public Vector3 moveInput;
        public bool isSneaking;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public Vector3? physicsPosition;
        public Vector3 LeanTarget = Vector3.zero;
        public Vector3 LeanSmoothed = Vector3.zero;
        private bool wasMovingLastFrame;

        public PC()
        {
        }

        public PC(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void Tick()
        {
            if (pawn.Downed ||
                !pawn.Spawned ||
                pawn.Map == null ||
                pawn.IsUnderAIControl() ||
                pawn.InMentalState)
            {
                return;
            }

            PreventJobExpiry();
            HandlePather();
        }

        private void HandlePather()
        {
            if (IsMoving && pawn.pather != null)
            {
                pawn.pather.lastMovedTick = Find.TickManager.TicksGame;

                float moveSpeed;
                if (isSneaking)
                {
                    moveSpeed = 4.6f;
                }
                else
                {
                    moveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed) * 0.7f;
                }

                pawn.pather.nextCellCostTotal =
                    Mathf.Max(
                        60f / Mathf.Max(moveSpeed, 0.1f),
                        1f);
            }
        }
        private void PreventJobExpiry()
        {
            if (pawn.jobs?.curJob != null &&
                pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait &&
                pawn.jobs.curJob.def != RimWorld.JobDefOf.Wait_Combat)
            {
                pawn.jobs.curJob.expiryInterval = -1;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(
                ref pawn,
                "pawn",
                saveDestroyedThings: true);

            Scribe_References.Look(
                ref savedLord,
                "savedLord");
        }
    }
}