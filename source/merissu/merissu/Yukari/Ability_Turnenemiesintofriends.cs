using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace merissu
{
    public class Ability_Turnenemiesintofriends : Ability
    {
        public Ability_Turnenemiesintofriends() : base() { }

        public Ability_Turnenemiesintofriends(Pawn pawn) : base(pawn) { }
        public Ability_Turnenemiesintofriends(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 5f)
                {
                    return "灵力不足 (需要5层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 5f) return false; 

            if (!base.Activate(target, dest)) return false;

            hp.Severity -= 5f;

            Map map = this.pawn.Map;
            if (map == null) return true;

            IEnumerable<Faction> validFactions = Find.FactionManager.AllFactionsListForReading
                .Where(f => !f.IsPlayer && !f.Hidden);

            List<Faction> hostileFactions = validFactions.Where(f => f.HostileTo(Faction.OfPlayer)).ToList();
            List<Faction> neutralFactions = validFactions.Where(f => !f.HostileTo(Faction.OfPlayer) && f.PlayerRelationKind == FactionRelationKind.Neutral).ToList();
            List<Faction> alliedFactions = validFactions.Where(f => f.PlayerRelationKind == FactionRelationKind.Ally).ToList();

            if (hostileFactions.NullOrEmpty())
            {
                if (Faction.OfMechanoids != null) hostileFactions.Add(Faction.OfMechanoids);
                else if (Faction.OfInsects != null) hostileFactions.Add(Faction.OfInsects);
            }

            List<Pawn> pawnsToProcess = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != Faction.OfPlayer && !p.Dead && !p.Destroyed)
                .ToList();

            int changedCount = 0;

            foreach (Pawn targetPawn in pawnsToProcess)
            {
                if (!targetPawn.RaceProps.Humanlike && !targetPawn.RaceProps.Animal && !targetPawn.RaceProps.IsMechanoid) continue;

                int relationChoice = Rand.RangeInclusive(0, 2);
                Faction targetFaction = null;

                if (relationChoice == 0 && !hostileFactions.NullOrEmpty())
                    targetFaction = hostileFactions.RandomElement();
                else if (relationChoice == 1)
                    targetFaction = targetPawn.RaceProps.Animal ? null : neutralFactions.RandomElementWithFallback(null);
                else if (relationChoice == 2)
                    targetFaction = alliedFactions.RandomElementWithFallback(neutralFactions.RandomElementWithFallback(null));

                if (targetFaction == null && !(targetPawn.RaceProps.Animal && relationChoice == 1)) continue;
                if (targetPawn.Faction == targetFaction) continue;

                Lord lord = targetPawn.GetLord();
                if (lord != null) lord.Notify_PawnLost(targetPawn, PawnLostCondition.ChangedFaction);

                targetPawn.SetFaction(targetFaction, null);
                changedCount++;

                if (targetPawn.Spawned)
                {
                    FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastSkipFlashEntry);
                    FleckMaker.ThrowDustPuff(targetPawn.Position, targetPawn.Map, 1.2f);
                    SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(targetPawn.Position, targetPawn.Map));
                }

                targetPawn.jobs?.StopAll();

                if (relationChoice > 0 && targetPawn.InMentalState)
                    targetPawn.mindState.mentalStateHandler.CurState.RecoverFromState();
                else if (relationChoice == 0 && targetPawn.RaceProps.Animal && targetPawn.Faction == null)
                    targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
            }

            if (changedCount > 0)
                Messages.Message($"结界「光明与黑暗的网目」：全场 {changedCount} 个单位的敌我界线已模糊...", MessageTypeDefOf.NeutralEvent, true);

            return true;
        }
    }
}