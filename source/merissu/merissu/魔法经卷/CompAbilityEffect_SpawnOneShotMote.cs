using RimWorld;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace merissu
{
    public class CompProperties_SpawnOneShotMote : CompProperties_AbilityEffect
    {
        public ThingDef moteDef;

        public CompProperties_SpawnOneShotMote()
        {
            compClass = typeof(CompAbilityEffect_SpawnOneShotMote);
        }
    }

    public class CompAbilityEffect_SpawnOneShotMote : CompAbilityEffect
    {
        public new CompProperties_SpawnOneShotMote Props => (CompProperties_SpawnOneShotMote)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            if (parent.pawn?.Spawned ?? false)
            {
                Mote_ShinkiRecitation mote = (Mote_ShinkiRecitation)ThingMaker.MakeThing(Props.moteDef);
                GenSpawn.Spawn(mote, parent.pawn.Position, parent.pawn.Map);
                mote.Attach(parent.pawn);
                mote.offset = new Vector3(0.2f, 0f, 0.5f);
                mote.exactPosition = parent.pawn.DrawPos + mote.offset; 
            }
        }
    }
}