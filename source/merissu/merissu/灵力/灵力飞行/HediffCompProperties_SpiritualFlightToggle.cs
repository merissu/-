using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace merissu
{
    public class HediffCompProperties_SpiritualFlightToggle : HediffCompProperties
    {
        public float drainPerTick = 0.0005f; 
        public string iconPathOn = "Material/PowerPoint";   
        public string iconPathOff = "Material/PowerPoint"; 

        public HediffCompProperties_SpiritualFlightToggle()
        {
            compClass = typeof(HediffComp_SpiritualFlightToggle);
        }
    }

    public class HediffComp_SpiritualFlightToggle : HediffComp
    {
        public bool flightEnabled = false;
        public HediffCompProperties_SpiritualFlightToggle Props => (HediffCompProperties_SpiritualFlightToggle)props;

        private static readonly HediffDef SpiritualFlyingHediffDef = HediffDef.Named("Hediff_SpiritualFlying");

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref flightEnabled, "spiritualFlightEnabled", false);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (flightEnabled)
            {
                Pawn pawn = parent.pawn;

                if (pawn.Downed)
                {
                    ToggleFlight(false);
                    return;
                }

                parent.Severity -= Props.drainPerTick;

                if (parent.Severity <= 0.1f)
                {
                    parent.Severity = 0.1f; 
                    ToggleFlight(false);
                    Messages.Message("灵力不足，飞行已取消。", pawn, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        public void ToggleFlight(bool turnOn)
        {
            flightEnabled = turnOn;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.health == null) return;

            Hediff flyingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(SpiritualFlyingHediffDef);
            var tracker = FlightCompatUtility.EnsureFlightTracker(pawn);

            if (flightEnabled)
            {
                if (flyingHediff == null)
                    pawn.health.AddHediff(SpiritualFlyingHediffDef);

                if (tracker != null && tracker.CanFlyNow)
                {
                    tracker.StartFlying();
                }

                if (pawn.CurJob != null)
                {
                    pawn.CurJob.flying = true;
                }
            }
            else
            {
                if (flyingHediff != null)
                    pawn.health.RemoveHediff(flyingHediff);

                if (tracker != null && tracker.Flying)
                {
                    bool hasOtherFlight = FlightCompatUtility.IsAnyFlightEnabled(pawn)
                                          || tracker.CanEverFly; 

                    if (!hasOtherFlight)
                    {
                        AccessTools.Method(typeof(Pawn_FlightTracker), "ForceLand")
                            .Invoke(tracker, null);
                    }
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (parent.pawn.Faction == Faction.OfPlayer && !parent.pawn.Dead)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "灵力飞行",
                    defaultDesc = "消耗灵力进入飞行状态。\n再次点击取消。\n当灵力不足0.1时自动降落。",
                    icon = ContentFinder<Texture2D>.Get(flightEnabled ? Props.iconPathOn : Props.iconPathOff, true),
                    isActive = () => flightEnabled,
                    toggleAction = () => ToggleFlight(!flightEnabled)
                };
            }
        }
    }
}