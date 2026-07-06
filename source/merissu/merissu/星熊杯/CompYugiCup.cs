using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;
using RimWorld;

namespace merissu
{
    public class JobDriver_TransformToDaiginjo : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);

            Toil waitToil = Toils_General.Wait(120);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return waitToil;

            Toil spawnToil = new Toil
            {
                initAction = delegate
                {
                    Thing daiginjo = ThingMaker.MakeThing(ThingDef.Named("Daiginjo"));
                    daiginjo.stackCount = 1;

                    if (!pawn.inventory.innerContainer.TryAdd(daiginjo))
                    {
                        GenPlace.TryPlaceThing(daiginjo, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }
                    HediffDef daiginjoHediff = HediffDef.Named("Daiginjoalcohol");
                    if (daiginjoHediff != null)
                    {
                        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(daiginjoHediff);
                        if (existing != null)
                            existing.Severity += 0.15f;
                        else
                            pawn.health.AddHediff(daiginjoHediff).Severity = 0.15f;
                    }

                    HediffDef toleranceDef = HediffDef.Named("AlcoholTolerance");
                    if (toleranceDef != null)
                    {
                        Hediff existingTol = pawn.health.hediffSet.GetFirstHediffOfDef(toleranceDef);
                        float addTol = 0.02f / Mathf.Max(pawn.BodySize, 1f);
                        if (existingTol != null)
                            existingTol.Severity += addTol;
                        else
                            pawn.health.AddHediff(toleranceDef).Severity = addTol;
                    }

                    foreach (Need need in pawn.needs.AllNeeds)
                    {
                        if (need is Need_Chemical chemNeed)
                            chemNeed.CurLevel = chemNeed.MaxLevel;
                    }

                    Messages.Message($"{pawn.LabelShort}将酒注入星熊杯，变成了纯米大吟酿", pawn, MessageTypeDefOf.PositiveEvent);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return spawnToil;
        }
    }

    public class CompYugiCup : ThingComp
    {
        private float alcoholLevel;

        public CompProperties_YugiCup Props => (CompProperties_YugiCup)props;

        public float AlcoholLevel => alcoholLevel;
        public float AlcoholPercent => alcoholLevel / Props.alcoholCapacity;
        public bool IsEmpty => alcoholLevel <= 0f;

        private Pawn Wearer
        {
            get
            {
                if (parent.ParentHolder is Pawn_EquipmentTracker eq)
                    return eq.pawn;
                return null;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref alcoholLevel, "alcoholLevel", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.IsHashIntervalTick(60))
                return;

            Pawn pawn = Wearer;
            if (pawn == null)
                return;

            if (IsEmpty)
            {
                TryAutoRefill(pawn);
                return;
            }

            float consume = Props.alcoholConsumptionPerTick * 60f;
            alcoholLevel = Mathf.Max(0f, alcoholLevel - consume);

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.intoxicationHediff);
            float addSev = Props.severityPerConsumption * consume;
            if (hediff != null)
                hediff.Severity += addSev;
            else
                pawn.health.AddHediff(Props.intoxicationHediff).Severity = addSev;

            ChemicalDef alcoholChem = ChemicalDefOf.Alcohol;
            HediffDef addictionDef = alcoholChem?.addictionHediff;
            if (addictionDef != null)
            {
                Hediff_Addiction existingAddiction = AddictionUtility.FindAddictionHediff(pawn, alcoholChem);
                if (existingAddiction == null)
                {
                    Hediff_Addiction addiction = (Hediff_Addiction)pawn.health.AddHediff(addictionDef);
                    addiction.Severity = addictionDef.maxSeverity;
                }
                else
                {
                    existingAddiction.Severity = Mathf.Min(
                        existingAddiction.Severity + 0.2f,
                        existingAddiction.def.maxSeverity
                    );
                }

                if (addictionDef.chemicalNeed != null &&
                    pawn.needs.TryGetNeed(addictionDef.chemicalNeed, out Need need))
                {
                    need.CurLevel = 1f;
                }
            }
        }

        private void TryAutoRefill(Pawn pawn)
        {
            if (pawn.inventory == null)
                return;

            for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
            {
                Thing thing = pawn.inventory.innerContainer[i];
                if (thing == null || thing.stackCount <= 0)
                    continue;

                if (!HasAlcoholDrugComp(thing.def))
                    continue;

                thing.SplitOff(1).Destroy();
                alcoholLevel = Props.alcoholCapacity;
                return;
            }
        }

        private bool HasAlcoholDrugComp(ThingDef def)
        {
            if (def.comps == null)
                return false;

            for (int j = 0; j < def.comps.Count; j++)
            {
                if (def.comps[j] is CompProperties_Drug drug && drug.chemical == ChemicalDefOf.Alcohol)
                    return true;
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            return "酒精: " + alcoholLevel.ToString("F1") + " / " + Props.alcoholCapacity.ToString("F1");
        }
    }

    [StaticConstructorOnStartup]
    public static class YugiCupHarmonyInit
    {
        static YugiCupHarmonyInit()
        {
            Harmony harmony = new Harmony("merissu.yugicup");

            harmony.Patch(
                AccessTools.Method(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick"),
                postfix: new HarmonyMethod(typeof(Patch_EquipmentTrackerTick), nameof(Patch_EquipmentTrackerTick.Postfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(CompEquippable), "CompGetEquippedGizmosExtra"),
                postfix: new HarmonyMethod(typeof(Patch_CompGetEquippedGizmosExtra), nameof(Patch_CompGetEquippedGizmosExtra.Postfix))
            );
        }
    }

    public static class Patch_EquipmentTrackerTick
    {
        public static void Postfix(Pawn_EquipmentTracker __instance)
        {
            List<ThingWithComps> list = __instance.AllEquipmentListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                CompYugiCup cup = list[i].GetComp<CompYugiCup>();
                if (cup != null)
                    cup.CompTick();
            }
        }
    }

    public static class Patch_CompGetEquippedGizmosExtra
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompEquippable __instance)
        {
            foreach (Gizmo g in __result)
                yield return g;

            if (__instance.parent?.def?.defName != "YugiCup")
                yield break;

            CompYugiCup cupComp = __instance.parent.TryGetComp<CompYugiCup>();
            if (cupComp == null)
                yield break;

            Pawn pawn = null;
            if (__instance.parent.ParentHolder is Pawn_EquipmentTracker eq)
                pawn = eq.pawn;

            if (pawn == null || pawn.Faction != Faction.OfPlayer)
                yield break;

            if (Find.Selector.SelectedObjects.Count == 1)
            {
                yield return new Gizmo_YugiCupStatus { cup = cupComp };
            }

            bool hasAlcohol = false;
            if (pawn.inventory != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    if (thing != null && thing.stackCount > 0 && HasAlcoholDrugComp(thing.def))
                    {
                        hasAlcohol = true;
                        break;
                    }
                }
            }

            yield return new Command_Action
            {
                defaultLabel = "转化大吟酿",
                defaultDesc = "消耗身上的一件酒，花 2 秒转化为纯米大吟酿。",
                icon = __instance.parent.def.uiIcon,
                action = delegate
                {
                    Thing alcoholThing = null;
                    for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                    {
                        Thing thing = pawn.inventory.innerContainer[i];
                        if (thing != null && thing.stackCount > 0 && HasAlcoholDrugComp(thing.def))
                        {
                            alcoholThing = thing;
                            break;
                        }
                    }

                    if (alcoholThing == null)
                    {
                        Messages.Message("身上没有酒。", pawn, MessageTypeDefOf.RejectInput);
                        return;
                    }

                    alcoholThing.SplitOff(1).Destroy();

                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Job_TransformToDaiginjo"), __instance.parent);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            };
        }

        private static bool HasAlcoholDrugComp(ThingDef def)
        {
            if (def.comps == null)
                return false;

            for (int j = 0; j < def.comps.Count; j++)
            {
                if (def.comps[j] is CompProperties_Drug drug && drug.chemical == ChemicalDefOf.Alcohol)
                    return true;
            }
            return false;
        }
    }

    public class Gizmo_YugiCupStatus : Gizmo
    {
        private static readonly Texture2D FillTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.4f, 0.1f));
        private static readonly Texture2D EmptyTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.2f));
        public CompYugiCup cup;

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);

            Text.Font = GameFont.Tiny;
            Widgets.Label(rect2, "星熊杯剩余酒量");

            Rect barRect = new Rect(rect2.x, rect2.y + 18f, rect2.width, 16f);
            Widgets.FillableBar(barRect, cup.AlcoholPercent, FillTex, EmptyTex, false);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, cup.AlcoholLevel.ToString("F1") + " / " + cup.Props.alcoholCapacity.ToString("F1"));
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}