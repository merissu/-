using RimWorld;
using Verse;
using Verse.AI; 

namespace merissu
{
    public class WildlyStrikes : Ability
    {
        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");
        private static readonly JobDef WildlyStrikesJob = DefDatabase<JobDef>.GetNamed("Job_WildlyStrikes");
        public WildlyStrikes() : base() { }

        public WildlyStrikes(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                if (pawn.Downed || pawn.Dead) return false; 

                if (pawn.equipment == null || pawn.equipment.Primary == null || !pawn.equipment.Primary.def.IsMeleeWeapon)
                {
                    return "必须装备绯想之剑";
                }

                Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
                if (fp == null || fp.Severity < 1f)
                {
                    return "灵力不足（需要1层）";
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff fp = pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);
            if (fp == null || fp.Severity < 1f)
                return false;

            fp.Severity -= 1f;

            Job job = JobMaker.MakeJob(WildlyStrikesJob, target.Thing);
            job.playerForced = true;

            pawn.jobs.StopAll();
            pawn.jobs.ClearQueuedJobs();
            pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);

            return true;
        }
    }
}