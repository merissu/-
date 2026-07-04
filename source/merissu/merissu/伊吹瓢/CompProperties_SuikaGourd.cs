using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static merissu.CompSuikaGourd;

namespace merissu
{
    public class IngestionOutcomeDoer_SatisfyAllChemicals : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            foreach (Need need in pawn.needs.AllNeeds)
            {
                if (need is Need_Chemical chemNeed)
                {
                    chemNeed.CurLevel = chemNeed.MaxLevel;
                }
            }
        }
    }

    public class CompProperties_SuikaEquippable : CompProperties
    {
        public CompProperties_SuikaEquippable()
        {
            compClass = typeof(CompSuikaEquippable);
        }
    }

    public class JobDriver_PourSuikaSake : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            Toil waitToil = Toils_General.Wait(60);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return waitToil;

            Toil spawnToil = new Toil
            {
                initAction = delegate
                {
                    Thing sake = ThingMaker.MakeThing(ThingDef.Named("OniKillerSake"));
                    sake.stackCount = 1;

                    if (!pawn.inventory.innerContainer.TryAdd(sake))
                    {
                        GenPlace.TryPlaceThing(sake, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }

                    Messages.Message($"{pawn.LabelShort}从伊吹瓢中倒出了一杯鬼杀酒。", pawn, MessageTypeDefOf.PositiveEvent);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return spawnToil;
        }
    }

    public class CompProperties_SuikaGourd : CompProperties
    {
        public string pourIconPath = "";

        public CompProperties_SuikaGourd()
        {
            this.compClass = typeof(CompSuikaGourd);
        }
    }

    public class CompSuikaGourd : ThingComp
    {
        public CompProperties_SuikaGourd Props => (CompProperties_SuikaGourd)this.props;


        public class CompSuikaEquippable : CompEquippable
        {
            public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
            {
                foreach (var g in base.CompGetEquippedGizmosExtra()) yield return g;

                CompSuikaGourd gourdComp = parent.GetComp<CompSuikaGourd>();
                if (gourdComp == null) yield break;

                Pawn pawn = Holder;
                if (pawn != null && pawn.Faction == Faction.OfPlayer)
                {
                    Texture2D buttonIcon = parent.def.uiIcon;
                    if (!string.IsNullOrEmpty(gourdComp.Props.pourIconPath))
                        buttonIcon = ContentFinder<Texture2D>.Get(gourdComp.Props.pourIconPath, true) ?? parent.def.uiIcon;

                    yield return new Command_Action
                    {
                        defaultLabel = "倒酒",
                        defaultDesc = "让小人倒出一份鬼杀酒。",
                        icon = buttonIcon,
                        action = delegate
                        {
                            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Job_PourSuikaSake"), parent);
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    };
                }
            }
        }
    }

    public class IngestionOutcomeDoer_RecreateGourd : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            Thing newGourd = ThingMaker.MakeThing(ingested.def, ingested.Stuff);
            newGourd.HitPoints = ingested.HitPoints;
            newGourd.TryGetComp<CompQuality>()?.SetQuality(ingested.TryGetComp<CompQuality>()?.Quality ?? QualityCategory.Normal, ArtGenerationContext.Colony);

            if (!pawn.inventory.innerContainer.TryAdd(newGourd))
                GenPlace.TryPlaceThing(newGourd, pawn.Position, pawn.Map, ThingPlaceMode.Near);

            foreach (Need need in pawn.needs.AllNeeds)
            {
                if (need is Need_Chemical chemNeed) chemNeed.CurLevel = chemNeed.MaxLevel;
            }
        }
    }
}