using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Ability_SubterraneanSun : Ability
    {
        public Ability_SubterraneanSun() : base() { }

        public Ability_SubterraneanSun(Pawn pawn, AbilityDef def) : base(pawn, def) { }

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
            if (pawn == null || pawn.Map == null)
                return false;

            if (!target.IsValid || !target.Cell.InBounds(pawn.Map))
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp == null || hp.Severity < 1f)
            {
                return false; 
            }
            hp.Severity -= 1f; 

            Thing sun = ThingMaker.MakeThing(ThingDef.Named("SubterraneanSunThing"));
            if (sun is Thing_SubterraneanSun sunThing)
            {
                sunThing.centerCell = target.Cell;
                GenSpawn.Spawn(sunThing, target.Cell, pawn.Map);
                SoundDef.Named("MegaFlare2").PlayOneShot(SoundInfo.InMap(pawn));
            }

            return true;
        }
    }
}