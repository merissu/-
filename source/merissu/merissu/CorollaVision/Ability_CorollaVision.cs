using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class Ability_CorollaVision : Ability
    {
        private const int TotalShots = 36;
        private const int ShotInterval = 6;
        private const float MaxRange = 25f;

        private static readonly HediffDef FullPowerDef = DefDatabase<HediffDef>.GetNamed("FullPower");

        private int shotsLeft;
        private int ticksToNextShot;
        private Vector3 shootDir;
        private bool firing;
        public Ability_CorollaVision() : base() { }

        public Ability_CorollaVision(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
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
            if (fp != null && fp.Severity >= 1f)
            {
                fp.Severity -= 1f; 

                Vector3 start = pawn.DrawPos;
                Vector3 targetPos = target.Cell.ToVector3Shifted();

                shootDir = targetPos - start;
                shootDir.y = 0f;

                if (shootDir.magnitude > MaxRange)
                    shootDir = shootDir.normalized * MaxRange;

                shootDir.Normalize();

                shotsLeft = TotalShots;
                ticksToNextShot = 0;
                firing = true;

                return base.Activate(target, dest); 
            }

            return false; 
        }

        public override void AbilityTick()
        {
            base.AbilityTick();

            if (!firing) return;

            if (ticksToNextShot > 0)
            {
                ticksToNextShot--;
                return;
            }

            FireOne();
            shotsLeft--;
            ticksToNextShot = ShotInterval;

            if (shotsLeft <= 0)
                firing = false;
        }

        private void FireOne()
        {
            Thing_CorollaVisionRing ring =
                (Thing_CorollaVisionRing)ThingMaker.MakeThing(
                    ThingDef.Named("CorollaVisionRing"));

            ring.Init(pawn, pawn.DrawPos, shootDir);
            GenSpawn.Spawn(ring, pawn.Position, pawn.Map);
        }
    }
}