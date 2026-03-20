using RimWorld;
using System.Collections.Generic;
using Verse;

namespace merissu
{
    public class HediffComp_MadBerserkExplosion : HediffComp
    {
        private int tickCounter = 0;
        private bool musicStarted = false;

        public HediffCompProperties_MadBerserkExplosion Props =>
            (HediffCompProperties_MadBerserkExplosion)props;

        public override void CompPostMake()
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null)
                return;

            if (Props.songDef != null)
            {
                Find.MusicManagerPlay.ForcePlaySong(Props.songDef, true);
                musicStarted = true;
            }

            GenExplosion.DoExplosion(
                pawn.Position,
                pawn.Map,
                Props.explosionRadius,
                Props.damageDef,
                null,
                Props.damageAmount,
                armorPenetration: 0f,
                ignoredThings: new List<Thing> { pawn }
            );

            pawn.mindState.mentalStateHandler.TryStartMentalState(
                MentalStateDefOf.Berserk,
                "因「那种事情」而精神崩溃",
                true,
                false,
                true
            );
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null)
                return;

            tickCounter++;

            if (tickCounter >= 300)
            {
                tickCounter = 0;

                GenExplosion.DoExplosion(
                    pawn.Position,
                    pawn.Map,
                    Props.explosionRadius,
                    Props.damageDef,
                    null,
                    Props.damageAmount,
                    armorPenetration: 0f,
                    ignoredThings: new List<Thing> { pawn }
                );
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (musicStarted)
            {
                Find.MusicManagerPlay.ForcePlaySong(null, false);
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref musicStarted, "musicStarted", false);
        }
    }
}
