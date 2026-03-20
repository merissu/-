using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Ability_FinalMasterSpark : Ability
    {
        private const int PhaseOneTicks = 150;
        private const int PhaseTwoTicks = 150;
        private const int PhaseThreeTicks = 60;
        private const int TotalDurationTicks = PhaseOneTicks + PhaseTwoTicks + PhaseThreeTicks;

        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        public Ability_FinalMasterSpark() : base() { }

        public Ability_FinalMasterSpark(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < 4f)
                {
                    return "灵力不足（需要4层）";
                }
                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null || !target.IsValid)
                return false;

            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < 4f)
            {
                Messages.Message("灵力不足", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            fp.Severity -= 4f; 

            Hediff cardStatus = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("MarisaCardDeclared"));
            if (cardStatus != null) pawn.health.RemoveHediff(cardStatus);

            HediffDef myStatus = HediffDef.Named("FinalMasterSpark");
            if (myStatus != null && !pawn.health.hediffSet.HasHediff(myStatus)) pawn.health.AddHediff(myStatus);

            SoundDef.Named("MasterSpark").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            Vector3 dirVec = (target.Cell - pawn.Position).ToVector3();
            if (dirVec.sqrMagnitude < 0.01f) dirVec = pawn.Rotation.FacingCell.ToVector3();
            dirVec.Normalize();

            Thing laser = ThingMaker.MakeThing(ThingDef.Named("FinalMasterSparkLaser"));
            if (laser is Thing_FinalMasterSparkLaser laserThing)
            {
                laserThing.direction = dirVec;
                laserThing.caster = pawn;
                GenSpawn.Spawn(laserThing, pawn.Position, pawn.Map);
            }

            pawn.stances.SetStance(new Stance_FinalMasterSpark(TotalDurationTicks, target.Cell, null));
            pawn.jobs.StopAll();
            Job waitJob = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, TotalDurationTicks);
            pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);

            return true;
        }
    }
    public class Stance_FinalMasterSpark : Stance_Busy
    {
        public Stance_FinalMasterSpark() { }
        public Stance_FinalMasterSpark(int ticks, LocalTargetInfo focus, Verb verb) : base(ticks, focus, verb) { }

        public override void StanceTick()
        {
            this.ticksLeft--;
            if (this.ticksLeft <= 0)
            {
                this.Expire();
            }
        }
        public override void StanceDraw() { }
    }
}