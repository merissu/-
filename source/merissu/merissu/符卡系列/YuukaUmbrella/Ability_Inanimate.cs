using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Ability_Inanimate : Ability
    {
        private const int LaserDurationTicks = 300;
        public Ability_Inanimate() : base() { }

        public Ability_Inanimate(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null || !target.IsValid)
                return false;

            SoundDef.Named("MasterSpark").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            Vector3 dirVec = (target.Cell - pawn.Position).ToVector3();
            if (dirVec.sqrMagnitude < 0.01f) dirVec = pawn.Rotation.FacingCell.ToVector3();
            dirVec.Normalize();

            Thing laser = ThingMaker.MakeThing(ThingDef.Named("InanimateLaser"));
            if (laser is InanimateLaser laserThing)
            {
                laserThing.direction = dirVec;
                laserThing.caster = pawn;
                GenSpawn.Spawn(laserThing, pawn.Position, pawn.Map);
            }

            pawn.stances.SetStance(new Stance_FinalMasterSpark(LaserDurationTicks, target.Cell, null));

            pawn.jobs.StopAll();
            Job waitJob = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, LaserDurationTicks);
            pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);

            return true;
        }
    }

    public class Stance_Inanimate : Stance_Busy
    {
        public Stance_Inanimate() { }
        public Stance_Inanimate(int ticks, LocalTargetInfo focus, Verb verb) : base(ticks, focus, verb) { }

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