using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class CompAbilityEffect_Gap : CompAbilityEffect_WithDest
    {
        public static string SkipUsedSignalTag = "CompAbilityEffect.SkipUsed";

        public new CompProperties_AbilityTeleport Props => (CompProperties_AbilityTeleport)props;

        private static readonly SoundDef GapOneSound = SoundDef.Named("gapone");
        private static readonly SoundDef GapTwoSound = SoundDef.Named("gaptwo");
        private static readonly SoundDef GapKillSound = SoundDef.Named("gapkill");

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    Map map = parent.pawn.Map;

                    GapOneSound?.PlayOneShot(new TargetInfo(t.Cell, map));

                    if (!parent.def.HasAreaOfEffect)
                    {
                        Pawn pawn = t.Pawn;
                        if (pawn != null)
                        {
                            FleckCreationData dataAttachedOverlay =
                                FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry,
                                    new Vector3(-0.5f, 0f, -0.5f));

                            dataAttachedOverlay.link.detachAfterTicks = 5;
                            pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                        }
                        else
                        {
                            FleckMaker.Static(t.CenterVector3, map, FleckDefOf.PsycastSkipFlashEntry);
                        }

                        FleckMaker.Static(d.Cell, map, FleckDefOf.PsycastSkipInnerExit);
                    }

                    if (Props.destination != AbilityEffectDestination.RandomInRange)
                    {
                        FleckMaker.Static(d.Cell, map, FleckDefOf.PsycastSkipOuterRingExit);
                    }
                },
                ticksAwayFromCast = 5
            };
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.HasThing)
                return;

            base.Apply(target, dest);

            Pawn pawn = target.Pawn;
            Map map = parent.pawn.Map;

            if (target == dest)
            {
                GapKillSound?.PlayOneShot(new TargetInfo(target.Cell, map));

                FleckMaker.Static(target.Cell, map, FleckDefOf.PsycastSkipFlashEntry);
                FleckMaker.Static(target.Cell, map, FleckDefOf.PsycastSkipInnerExit);
                FleckMaker.ThrowDustPuff(target.Cell, map, 2f);

                Effecter effecter = RimWorld.EffecterDefOf.Skip_Entry.Spawn();
                effecter.Trigger(new TargetInfo(target.Cell, map), new TargetInfo(target.Cell, map));
                effecter.Cleanup();

                target.Thing.Destroy();

                if (pawn != null)
                {
                    Find.WorldPawns.RemovePawn(pawn);
                }

                return;
            }

            GapTwoSound?.PlayOneShot(new TargetInfo(dest.Cell, map));

            LocalTargetInfo destination = GetDestination(dest.IsValid ? dest : target);
            if (!destination.IsValid)
                return;

            Pawn casterPawn = parent.pawn;

            parent.AddEffecterToMaintain(
                RimWorld.EffecterDefOf.Skip_Entry.Spawn(target.Thing, map),
                target.Thing.Position,
                60);

            parent.AddEffecterToMaintain(
                RimWorld.EffecterDefOf.Skip_Exit.Spawn(destination.Cell, map),
                destination.Cell,
                60);

            target.Thing.TryGetComp<CompCanBeDormant>()?.WakeUp();
            target.Thing.Position = destination.Cell;

            if (target.Thing is Pawn transportedPawn)
            {
                if ((transportedPawn.Faction == Faction.OfPlayer || transportedPawn.IsPlayerControlled)
                    && transportedPawn.Position.Fogged(map))
                {
                    FloodFillerFog.FloodUnfog(transportedPawn.Position, map);
                }

                transportedPawn.stances.stunner.StunFor(
                    Props.stunTicks.RandomInRange,
                    parent.pawn,
                    addBattleLog: false,
                    showMote: false);

                transportedPawn.Notify_Teleported();
                CompAbilityEffect_Teleport.SendSkipUsedSignal(
                    transportedPawn.Position,
                    transportedPawn);
            }

            if (Props.destClamorType != null)
            {
                GenClamor.DoClamor(
                    casterPawn,
                    target.Cell,
                    Props.destClamorRadius,
                    Props.destClamorType);
            }
        }
    }
}
