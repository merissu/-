using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class Ability_Hisouten : Ability
    {
        private const int TotalDurationTicks = 240;
        public Ability_Hisouten() : base() { }

        public Ability_Hisouten(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || !target.IsValid) return false;

            Vector3 dirVec = (target.Cell - pawn.Position).ToVector3();
            if (dirVec.sqrMagnitude < 0.01f) dirVec = pawn.Rotation.FacingCell.ToVector3();
            dirVec.Normalize();

            Thing_HisoutenLaser laser = (Thing_HisoutenLaser)ThingMaker.MakeThing(ThingDef.Named("HisoutenLaser"));
            laser.direction = dirVec;
            laser.caster = pawn;
            GenSpawn.Spawn(laser, pawn.Position, pawn.Map);

            pawn.stances.SetStance(new Stance_Hisouten(TotalDurationTicks, target.Cell, null));
            pawn.jobs.StopAll();
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, TotalDurationTicks), JobTag.Misc);

            return true;
        }
    }

    public class Stance_Hisouten : Stance_Busy
    {
        public Stance_Hisouten() { }
        public Stance_Hisouten(int ticks, LocalTargetInfo focus, Verb verb) : base(ticks, focus, verb) { }

        public override void StanceDraw()
        {
        }

        public override void StanceTick()
        {
            base.StanceTick();
        }
    }
}