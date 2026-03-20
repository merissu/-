using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace U.N.Owen.EndAbout
{
    [DefOf]
    public static class JobDefOf
    {
        static JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
        public static JobDef OpenGensokyo;
        public static JobDef SealingSupernaturalBead;
    }
    [DefOf]
    public static class SongDefOf
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        static SongDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SongDefOf));
        }
        public static SongDef Eastern_Heaven_of_Scarlet_Perception;
    }
    [DefOf]
    public static class ThingDefOf
    {
        static ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
        }
        public static ThingDef ShrineBeacon;
        public static ThingDef SupernaturalBead;
    }
    public class JobDriver_SealingSupernaturalBead : JobDriver_Goto
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Thing thing = this.job.GetTarget(TargetIndex.A).Thing;
            Thing thing2 = this.job.GetTarget(TargetIndex.B).Thing;
            Thing thing3 = ThingMaker.MakeThing(ThingDefOf.ShrineBeacon);
            IntVec3 intVec = new IntVec3(thing.Position.x, thing.Position.y, thing.Position.z);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.AddFailCondition(() => !job.GetTarget(TargetIndex.A).HasThing);
            Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B, 1, 1, null, false);
            yield return reserveFuel;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, false).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false, true, false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None, true, null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
            yield return Toils_General.Wait(0.55f.SecondsToTicks(), TargetIndex.None);
            Toil toil = Toils_General.Wait(1.5f.SecondsToTicks(), TargetIndex.None);
            yield return toil;
            yield return Toils_General.Do(delegate
            {

                thing.Destroy();
                thing2.Destroy();
                GenSpawn.Spawn(thing3, intVec, this.Map);
            });
            yield return Toils_General.Wait(0.35f.SecondsToTicks(), TargetIndex.None);
            yield break;
        }
        private const TargetIndex ChargerInd = TargetIndex.A;
    }
    public class CompProperties_ShrineBeacon : CompProperties
    {
        // Token: 0x06005824 RID: 22564 RVA: 0x001E3D60 File Offset: 0x001E1F60
        public CompProperties_ShrineBeacon()
        {
            this.compClass = typeof(CompShrineBeacon);
        }
        public string jobString = "封印灵异珠";
    }
    public class CompShrineBeacon : ThingComp

    {
        private CompProperties_ShrineBeacon Props
        {
            get
            {
                return (CompProperties_ShrineBeacon)this.props;
            }
        }
        public LocalTargetInfo FindClosestFood(Pawn pawn, ThingDef thing)
        {
            return HoshigumasCup.CompExchange.FindClosestThingToPawn(
                pawn,
                ThingRequest.ForDef(thing),
                maxDistance: 99999f
            );
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            LocalTargetInfo target1 = FindClosestFood(selPawn, ThingDefOf.SupernaturalBead);
            if (hasSupernaturalBead || target1 == null)
            {
                yield break;
            }
            else
            {
                if (selPawn.CurJob != null && selPawn.CurJob.def == JobDefOf.SealingSupernaturalBead && selPawn.CurJob.targetA.Thing == this.parent)
                {
                    yield return new FloatMenuOption(this.Props.jobString, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                    yield break;
                }
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(this.Props.jobString, delegate ()
                {
                    Job job = JobMaker.MakeJob(JobDefOf.SealingSupernaturalBead, this.parent, target1);
                    selPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0), selPawn, this.parent, "ReservedBy", null);
            }
            yield break;
        }

        // Token: 0x0600582F RID: 22575 RVA: 0x001E3F9B File Offset: 0x001E219B
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield break;
        }
        private static Texture2D colonistOnlyCommandTex;
        public CompNeuralSupercharger.AutoUseMode autoUseMode = CompNeuralSupercharger.AutoUseMode.AutoUseWithDesire;
        public bool hasSupernaturalBead;
        private Effecter effecterCharged;
    }
    public class JobDriver_OpenGensokyo : JobDriver_Goto
    {
        private const int DefaultDuration = 120;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            Toil activateToil = ToilMaker.MakeToil("ActivateGenso");
            activateToil.initAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
                Thing target = job.targetA.Thing;
                CompEntryGenso comp = target != null ? target.TryGetComp<CompEntryGenso>() : null;
                if (comp != null)
                {
                    comp.Activate();
                }
            };
            activateToil.handlingFacing = true;
            activateToil.defaultCompleteMode = ToilCompleteMode.Delay;
            activateToil.defaultDuration = DefaultDuration;
            activateToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);

            yield return activateToil;
        }
    }

    public class CompProperties_EntryGenso : CompProperties
    {
        public int requiredLinkedFacilities = 7;

        // 结局CG配置（数量、来源、每张时长都在这里）
        public List<GensoCgStageDef> endingCgs = new List<GensoCgStageDef>();

        public SongDef endingSong;

        public string endingIntro =
            "一道深邃可怕的空间裂隙在你的眼前缓缓张开，露出无数猩红的眼瞳，此刻已无退路，也不必后退";

        public string endingText =
            "你走进了这道裂隙，而里边的景色却与你刚刚看到的截然不同，你发现你再次站在一座同样的鸟居下，但你抬头遥望，熟悉的殖民地已经无影无踪，你看见绯红的洋馆倒映在蔚蓝的湖面，你看见瀑布从山顶垂落，你看见金黄的花田连绵不断，你已经明白自己身在何处。身后传来少女不满的质问，回首看时，红白巫女服在空中轻轻飘荡，而她身后，这个不可思议的世界会静静地将你包容。";

        public float creditsSongStartDelay = 2.5f;

        public CompProperties_EntryGenso()
        {
            compClass = typeof(CompEntryGenso);
        }
    }

    public class GensoCgStageDef
    {
        public string texturePath = "CG/ED1";

        // 该图显示时长
        public float displaySeconds = 5f;

        // 图前白屏（淡入白 / 淡出白）
        public float preFadeToWhiteSeconds = 0.5f;
        public float preFadeFromWhiteSeconds = 0.5f;

        // 图后白屏（淡入白 / 淡出白）
        public float postFadeToWhiteSeconds = 0.5f;
        public float postFadeFromWhiteSeconds = 0.5f;
    }

    public class CompEntryGenso : ThingComp
    {
        private bool activated;
        private bool ended;
        private float startRealTime = -1f;
        private GensoEndingTimeline timeline;

        private CompProperties_EntryGenso Props
        {
            get { return (CompProperties_EntryGenso)props; }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!activated || timeline == null || ended)
            {
                return;
            }

            float elapsed = Time.realtimeSinceStartup - startRealTime;
            timeline.UpdateFades(elapsed);

            if (elapsed >= timeline.TotalDuration)
            {
                ended = true;
                EndGame();
            }
        }

        public void Activate()
        {
            if (activated)
            {
                return;
            }

            if (Props.endingCgs == null || Props.endingCgs.Count == 0)
            {
                Log.Warning("[CompEntryGenso] endingCgs is empty. Ending sequence will not play.");
                return;
            }

            timeline = new GensoEndingTimeline(Props.endingCgs);
            startRealTime = Time.realtimeSinceStartup;

            Find.WindowStack.Add(new Window_GensoEnding(timeline, startRealTime));

            SongDef song = Props.endingSong ?? SongDefOf.Eastern_Heaven_of_Scarlet_Perception;
            if (song != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(song, false);
            }

            activated = true;

            // 立刻触发 t=0 的淡入事件
            timeline.UpdateFades(0f);
        }

        public bool CountToActivate()
        {
            CompAffectedByFacilities fac = parent.TryGetComp<CompAffectedByFacilities>();
            if (fac == null || fac.LinkedFacilitiesListForReading == null)
            {
                return false;
            }
            return fac.LinkedFacilitiesListForReading.Count >= Props.requiredLinkedFacilities;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (CountToActivate())
            {
                yield return new FloatMenuOption("在博丽大结界上打开裂缝", delegate
                {
                    Job job = JobMaker.MakeJob(JobDefOf.OpenGensokyo, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        private void EndGame()
        {
            if (parent == null || parent.Map == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            List<Pawn> escaped = (from p in parent.Map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                                  where p.RaceProps.Humanlike
                                  select p).ToList();

            for (int i = 0; i < escaped.Count; i++)
            {
                Pawn pawn = escaped[i];
                if (!pawn.Dead && !pawn.IsQuestLodger())
                {
                    sb.AppendLine("   " + pawn.LabelCap);
                    Find.StoryWatcher.statsRecord.colonistsLaunched++;
                }
            }

            string credits = GameVictoryUtility.MakeEndCredits(
                Props.endingIntro.Translate(),
                Props.endingText.Translate(),
                sb.ToString(),
                "GameOverColonistsEntryGenso",
                escaped
            );

            GameVictoryUtility.ShowCredits(credits, null, true, Props.creditsSongStartDelay);
        }
    }

    public class GensoEndingTimeline
    {
        private struct FadeEvent
        {
            public float triggerAt;
            public Color color;
            public float duration;
        }

        private class StageRuntime
        {
            public Texture2D texture;
            public float showStart;
            public float showEnd;
        }

        private readonly List<FadeEvent> fadeEvents = new List<FadeEvent>();
        private readonly List<StageRuntime> stages = new List<StageRuntime>();
        private int nextFadeEventIndex;

        public float TotalDuration { get; private set; }

        public GensoEndingTimeline(List<GensoCgStageDef> defs)
        {
            float t = 0f;

            for (int i = 0; i < defs.Count; i++)
            {
                GensoCgStageDef def = defs[i];
                if (def == null) continue;

                // 前白屏：淡入白 -> 淡出白
                AddFade(t, Color.white, def.preFadeToWhiteSeconds);
                t += Mathf.Max(0f, def.preFadeToWhiteSeconds);

                AddFade(t, Color.clear, def.preFadeFromWhiteSeconds);
                t += Mathf.Max(0f, def.preFadeFromWhiteSeconds);

                Texture2D tex = ContentFinder<Texture2D>.Get(def.texturePath, true);
                float showStart = t;
                float showEnd = t + Mathf.Max(0f, def.displaySeconds);

                stages.Add(new StageRuntime
                {
                    texture = tex,
                    showStart = showStart,
                    showEnd = showEnd
                });

                t = showEnd;

                // 后白屏：淡入白 -> 淡出白
                AddFade(t, Color.white, def.postFadeToWhiteSeconds);
                t += Mathf.Max(0f, def.postFadeToWhiteSeconds);

                AddFade(t, Color.clear, def.postFadeFromWhiteSeconds);
                t += Mathf.Max(0f, def.postFadeFromWhiteSeconds);
            }

            TotalDuration = t;
        }

        public void UpdateFades(float elapsed)
        {
            while (nextFadeEventIndex < fadeEvents.Count && elapsed >= fadeEvents[nextFadeEventIndex].triggerAt)
            {
                FadeEvent e = fadeEvents[nextFadeEventIndex];
                ScreenFader.StartFade(e.color, e.duration);
                nextFadeEventIndex++;
            }
        }

        public Texture2D CurrentTexture(float elapsed)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                StageRuntime s = stages[i];
                if (elapsed >= s.showStart && elapsed < s.showEnd)
                {
                    return s.texture;
                }
            }
            return null;
        }

        private void AddFade(float triggerAt, Color color, float duration)
        {
            fadeEvents.Add(new FadeEvent
            {
                triggerAt = triggerAt,
                color = color,
                duration = Mathf.Max(0f, duration)
            });
        }
    }

    public class Window_GensoEnding : Window
    {
        private readonly GensoEndingTimeline timeline;
        private readonly float startRealTime;

        public Window_GensoEnding(GensoEndingTimeline timeline, float startRealTime)
        {
            this.timeline = timeline;
            this.startRealTime = startRealTime;

            doCloseButton = false;
            doCloseX = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Super;
            onlyOneOfTypeAllowed = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(UI.screenWidth, UI.screenHeight); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            float elapsed = Time.realtimeSinceStartup - startRealTime;
            Texture2D tex = timeline.CurrentTexture(elapsed);
            if (tex != null)
            {
                Rect imageRect = GetCenteredFitRect(inRect, tex.width, tex.height, 0.8f);
                GUI.DrawTexture(imageRect, tex, ScaleMode.ScaleToFit);
            }

            if (elapsed >= timeline.TotalDuration)
            {
                Close();
            }
        }

        private static Rect GetCenteredFitRect(Rect inRect, int texW, int texH, float scale)
        {
            float imageRatio = (float)texW / texH;
            float screenRatio = inRect.width / inRect.height;

            if (screenRatio > imageRatio)
            {
                float h = inRect.height * scale;
                float w = h * imageRatio;
                return new Rect((inRect.width - w) * 0.5f, (inRect.height - h) * 0.5f, w, h);
            }
            else
            {
                float w = inRect.width * scale;
                float h = w / imageRatio;
                return new Rect((inRect.width - w) * 0.5f, (inRect.height - h) * 0.5f, w, h);
            }
        }
    }
}

