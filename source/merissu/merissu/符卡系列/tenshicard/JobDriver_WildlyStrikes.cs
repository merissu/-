using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class JobDriver_WildlyStrikes : JobDriver
    {
        private int ticks;
        private int strikesDone;

        private const int StrikeInterval = 5;
        private const int MaxStrikes = 12;
        private const int TotalDurationTicks = 62;

        private static readonly SoundDef SoundA = SoundDef.Named("WildlyStrikesA");
        private static readonly SoundDef SoundB = SoundDef.Named("WildlyStrikesB");

        private Pawn TargetPawn => job.targetA.Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Dead || pawn.Downed);
            this.FailOn(() => pawn.equipment?.Primary == null || !pawn.equipment.Primary.def.IsMeleeWeapon);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil strikeToil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never
            };

            strikeToil.initAction = () =>
            {
                ticks = 0;
                strikesDone = 0;

                pawn.pather.StopDead();
                pawn.stances.SetStance(new Stance_Mobile());
            };

            strikeToil.tickAction = () =>
            {
                ticks++;
                Pawn actor = pawn;
                Pawn target = TargetPawn;

                if (target == null || target.Dead || target.Destroyed)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                actor.jobs.curDriver = this;

                if (ticks % StrikeInterval == 0 && strikesDone < MaxStrikes)
                {
                    strikesDone++;

                    bool isFinalStrike = (strikesDone == MaxStrikes);

                    DoOneSlash(actor, target, isFinalStrike);
                }

                if (strikesDone >= MaxStrikes || ticks >= TotalDurationTicks)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            };

            strikeToil.handlingFacing = true;
            yield return strikeToil;
        }

        private void DoOneSlash(Pawn actor, Pawn target, bool isLast)
        {
            if (target == null || target.Dead) return;

            Verb verb = actor.meleeVerbs.TryGetMeleeVerb(target);
            if (verb == null) return;

            if (isLast)
            {
                SoundB.PlayOneShot(new TargetInfo(target.Position, target.Map));
            }
            else
            {
                SoundA.PlayOneShot(new TargetInfo(target.Position, target.Map));
            }
            actor.stances.SetStance(new Stance_Mobile());
            verb.Reset();
            if (verb.TryStartCastOn(target))
            {
                actor.stances.SetStance(new Stance_Mobile());
            }
        }
    }
}