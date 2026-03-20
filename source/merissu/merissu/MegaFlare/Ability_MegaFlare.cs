using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Ability_MegaFlare : Ability
    {
        public Ability_MegaFlare() : base() { }

        public Ability_MegaFlare(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足 (需要1层)";
                }

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null || !target.IsValid) return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f)
            {
                return false;
            }

            hp.Severity -= 1f;

            Vector3 dir = (target.Cell - pawn.Position).ToVector3();
            if (dir.sqrMagnitude < 0.01f)
                dir = pawn.Rotation.FacingCell.ToVector3();
            dir.Normalize();

            SoundDef.Named("MegaFlare1").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            Thing thing = ThingMaker.MakeThing(ThingDef.Named("MegaFlareWarmup"));
            if (thing is Thing_MegaFlareWarmup warmup)
            {
                warmup.caster = pawn;
                warmup.direction = dir;
                GenSpawn.Spawn(warmup, pawn.Position, pawn.Map);
            }

            pawn.stances.SetStance(new Stance_MegaFlare(Thing_MegaFlareWarmup.WarmupDuration, target.Cell, null));
            pawn.pather.StopDead();

            Job waitJob = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, Thing_MegaFlareWarmup.WarmupDuration);
            pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);

            return true;
        }
    }

    public class Stance_MegaFlare : Stance_Busy
    {
        public Stance_MegaFlare() { }
        public Stance_MegaFlare(int ticks, LocalTargetInfo focus, Verb verb) : base(ticks, focus, verb) { }

        public override void StanceTick()
        {
            ticksLeft--;
            if (ticksLeft <= 0)
                Expire();
        }

        public override void StanceDraw() { }
    }
}