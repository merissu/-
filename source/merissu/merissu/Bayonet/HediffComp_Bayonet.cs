using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace merissu
{
    public class HediffComp_Bayonet : HediffComp
    {
        public HediffCompProperties_Bayonet Props =>
            (HediffCompProperties_Bayonet)props;

        public string str;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || pawn.Map == null)
            {
                return;
            }

            if (!str.NullOrEmpty() && pawn.IsHashIntervalTick(30))
            {
                MoteMaker.ThrowText(
                    pawn.DrawPos + new Vector3(0f, 0f, 0.75f),
                    pawn.Map,
                    str,
                    Color.white
                );
            }

            if (pawn.stances != null &&
                (pawn.stances.curStance is Stance_Cooldown ||
                 pawn.stances.curStance is Stance_Warmup))
            {
                pawn.stances.CancelBusyStanceHard();
            }

            if (pawn.jobs?.curJob != null &&
                pawn.CurJobDef != DefOf.MR_BayonetAttack)
            {
                pawn.health.RemoveHediff(parent);
                return;
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            Pawn pawn = Pawn;
            if (pawn != null && !pawn.Dead &&
                pawn.jobs?.curJob != null &&
                pawn.CurJobDef == DefOf.MR_BayonetAttack)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
