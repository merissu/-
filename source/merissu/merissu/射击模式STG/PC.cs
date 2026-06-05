using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using static merissu.STG_HitManager;

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
        private bool wasSneakingLastFrame;
        private float hitboxAppearProgress = 0f;
        private float hitboxRotation = 0f;
        private float hitboxCurrentAlpha = 0f;     
        private const float MaxHitboxAlpha = 0.5f; 
        private const float FadeSpeed = 5f;        

        private static readonly MaterialPropertyBlock _staticPropBlock = new MaterialPropertyBlock();
        private static readonly MaterialPropertyBlock _rotatingPropBlock = new MaterialPropertyBlock();
        public HashSet<int> grazedProjectileIds = new HashSet<int>();

        public List<GrazeParticle> grazeParticles = new List<GrazeParticle>();
        private static readonly HediffDef SpiritualPowerDef = HediffDef.Named("spiritualpower");
        private static readonly Material GrazeParticleMat = MaterialPool.MatFrom("UI/STG/GrazeItem", ShaderDatabase.TransparentPostLight);
        public PC()
        {
        }
        public void TryTriggerGraze(Thing proj)
        {
            if (grazedProjectileIds.Contains(proj.thingIDNumber)) return;

            grazedProjectileIds.Add(proj.thingIDNumber);

            SoundDef.Named("STG_Sound_Graze").PlayOneShot(SoundInfo.InMap(pawn));

            Vector3 spawnPos = physicsPosition ?? pawn.DrawPos;
            grazeParticles.Add(new GrazeParticle(spawnPos));

            if (SpiritualPowerDef != null && pawn.health != null)
            {
                Hediff spiritualHediff = pawn.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef);

                if (spiritualHediff == null)
                {
                    spiritualHediff = HediffMaker.MakeHediff(SpiritualPowerDef, pawn);

                    spiritualHediff.Severity = 0.011f;

                    pawn.health.AddHediff(spiritualHediff);
                }
                else
                {
                    spiritualHediff.Severity += 0.001f;
                }
            }
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
                    moveSpeed = 2.3f;
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