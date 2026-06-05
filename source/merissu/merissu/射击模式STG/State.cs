using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class State
    {
        private static int _pcPawnId = -1;
        private static int _isPCCacheFrame = -999;
        private static bool _isActiveCache;
        private static int _isActiveCacheFrame = -999;
        public static PC PC;
        public static bool DrawingTopRightGizmos = false;
        private static CameraMapConfig _savedConfig;
        public static Vector3? CameraLockPosition;
        public static bool skipDialog = false;
        public static bool IsUnderAIControl(this Pawn pawn)
        {
            return pawn.GetLord() != null ||
                   pawn.mindState?.duty != null;
        }

        public static bool IsActive
        {
            get
            {
                if (PC?.pawn == null)
                    return false;

                if (Time.frameCount != _isActiveCacheFrame)
                {
                    _isActiveCache =
                        PC.pawn != null &&
                        !PC.pawn.Dead &&
                        !WorldComponent_GravshipController.CutsceneInProgress;

                    _isActiveCacheFrame = Time.frameCount;
                }

                return _isActiveCache;
            }
        }

        public static float lastTickRealTime = 0f;

        public static float smoothedTickTime = 1f / 60f;

        public static bool ControlsFrozen
        {
            get
            {
                if (Find.WindowStack.WindowsPreventCameraMotion)
                    return true;

                if (GUI.GetNameOfFocusedControl() != "")
                    return true;

                if (PC?.pawn?.CurJob != null &&
                    !PC.pawn.CurJob.def.playerInterruptible)
                {
                    return true;
                }

                return false;
            }
        }

        public static void SetPC(Pawn pawn, bool showMessage = false)
        {
            if (PC?.pawn != null &&
                PC.pawn != pawn)
            {
                CleanupPawnState(PC.pawn);
            }

            PC = new PC(pawn);

            CameraLockPosition = null;

            if (pawn.jobs != null &&
                pawn.Spawned)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

                pawn.pather.StopDead();

                var wait = JobMaker.MakeJob(RimWorld.JobDefOf.Wait);

                wait.expiryInterval = 60;
                wait.checkOverrideOnExpire = true;

                pawn.jobs.TryTakeOrderedJob(wait);

                var lord = pawn.GetLord();

                if (lord != null)
                {
                    PC.savedLord = lord;

                    lord.Notify_PawnLost(
                        pawn,
                        PawnLostCondition.Undefined);
                }
            }

            if (showMessage)
            {
                Messages.Message(
                    $"已接管: {pawn.LabelShort}",
                    pawn,
                    MessageTypeDefOf.NeutralEvent);
            }
        }

        public static void ClearPC()
        {
            if (PC?.pawn != null)
            {
                CleanupPawnState(PC.pawn);
            }

            if (Find.CameraDriver != null)
            {
                if (_savedConfig != null)
                {
                    Find.CameraDriver.config = _savedConfig;
                    _savedConfig = null;
                }
                else
                {
                    Find.CameraDriver.config = new CameraMapConfig_Normal();

                    SimpleCameraBridge.ResetSimpleCamera();
                }
            }

            CameraLockPosition = null;
            PC = null;
            Cursor.visible = true;
        }
        private static void CleanupPawnState(Pawn pawn)
        {
            if (pawn.drafter == null)
                return;

            if (PC?.savedLord != null &&
                PC.savedLord.lordManager != null &&
                PC.savedLord.lordManager.lords.Contains(PC.savedLord))
            {
                PC.savedLord.AddPawn(pawn);

                PC.savedLord
                    .CurLordToil?
                    .UpdateAllDuties();
            }

            if (PC != null)
            {
                PC.savedLord = null;
            }
        }

        public static void RevokeControl(Pawn pawn)
        {
            if (PC?.pawn != pawn)
                return;

            ClearPC();

            Messages.Message(
                "解除控制",
                MessageTypeDefOf.NegativeEvent);
        }

        public static void Update()
        {
            if (!IsActive)
            {
                if (!Cursor.visible) Cursor.visible = true;
                return;
            }

            PC.RenderPawn();

            if (Find.TickManager.Paused) return;

            PC.UpdatePhysics();
            PC.UpdateCamera(); 
        }
        public static void Tick()
        {
            if (!IsActive)
                return;

            float now = Time.realtimeSinceStartup;

            if (lastTickRealTime != 0f)
            {
                float delta = now - lastTickRealTime;

                if (delta < 1f)
                {
                    smoothedTickTime =
                        Mathf.Lerp(
                            smoothedTickTime,
                            delta,
                            0.1f);
                }
            }

            lastTickRealTime = now;

            PC.Tick();
        }

        public static void OnGUI()
        {
            if (!IsActive)
            {
                if (!Cursor.visible)
                    Cursor.visible = true;

                return;
            }

            if (Find.CameraDriver == null)
                return;

            bool pawnSpawned = PC.pawn.Spawned;

            bool mapMatch =
                PC.pawn.Map != null &&
                PC.pawn.Map == Find.CurrentMap;

            bool pawnReady =
                pawnSpawned &&
                mapMatch;

            if (!pawnReady)
            {
                Cursor.visible = true;
                var container = TryGetSpawnedContainer(PC.pawn);

                if (container != null && container.Map == Find.CurrentMap)
                {
                    if (!(Find.CameraDriver.config is STGCamera))
                    {
                        _savedConfig = Find.CameraDriver.config;
                        Find.CameraDriver.config = new STGCamera();
                    }

                    Find.CameraDriver.rootPos = container.DrawPos;
                }

                return;
            }

            if (!(Find.CameraDriver.config is STGCamera))
            {
                _savedConfig =
                    Find.CameraDriver.config;

                Find.CameraDriver.config =
                    new STGCamera();
            }
        }

        public static Thing TryGetSpawnedContainer(Pawn pawn)
        {
            if (pawn == null)
                return null;

            IThingHolder holder =
                pawn.ParentHolder;

            while (holder != null)
            {
                if (holder is Thing t &&
                    t.Spawned)
                {
                    return t;
                }

                if (holder is ThingComp comp &&
                    comp.parent != null &&
                    comp.parent.Spawned)
                {
                    return comp.parent;
                }

                holder = holder.ParentHolder;
            }

            return null;
        }

        public static bool IsPC(this Pawn pawn)
        {
            if (Time.frameCount != _isPCCacheFrame)
            {
                _pcPawnId =
                    IsActive &&
                    PC?.pawn != null
                        ? PC.pawn.thingIDNumber
                        : -1;

                _isPCCacheFrame =
                    Time.frameCount;
            }

            return pawn.thingIDNumber ==
                   _pcPawnId;
        }

        public static void Message(string message)
        {
            Log.ResetMessageCount();
            Log.Message($"[STG] {message}");
        }

        public static void Warning(string message)
        {
            Log.ResetMessageCount();
            Log.Warning($"[STG] {message}");
        }

        public static void Error(string message)
        {
            Log.ResetMessageCount();
            Log.Error($"[STG] {message}");
        }
    }
}
