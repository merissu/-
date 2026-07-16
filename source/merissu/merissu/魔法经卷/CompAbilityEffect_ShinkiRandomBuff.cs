using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_ShinkiRandomBuff : CompProperties_AbilityEffect
    {
        public CompProperties_ShinkiRandomBuff()
        {
            compClass = typeof(CompAbilityEffect_ShinkiRandomBuff);
        }
    }

    public class CompAbilityEffect_ShinkiRandomBuff : CompAbilityEffect
    {
        private static readonly string[] BuffDefNames = new string[]
        {
            "ShinkiBuff_SpiritualRegen",
            "ShinkiBuff_MoveSpeed",
            "ShinkiBuff_Defense",
            "ShinkiBuff_Attack",
            "ShinkiBuff_Regen"
        };

        private static readonly string[] MoteDefNames = new string[]
        {
            "Mote_ShinkiBuff_A",
            "Mote_ShinkiBuff_B",
            "Mote_ShinkiBuff_C",
            "Mote_ShinkiBuff_D",
            "Mote_ShinkiBuff_E"
        };

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned) return;

            List<int> ownedIndices = new List<int>();
            for (int i = 0; i < BuffDefNames.Length; i++)
            {
                HediffDef def = HediffDef.Named(BuffDefNames[i]);
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                if (hediff != null && hediff.Severity > 0)
                    ownedIndices.Add(i);
            }

            if (ownedIndices.Count >= BuffDefNames.Length)
            {
                foreach (var defName in BuffDefNames)
                {
                    Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(defName));
                    if (h != null)
                        h.Severity = 1f;
                }

                HediffDef reward = HediffDef.Named("hijiriShinkiRecitation");
                if (reward != null)
                {
                    Hediff rewardHediff = HediffMaker.MakeHediff(reward, pawn);
                    rewardHediff.Severity = 1f;
                    pawn.health.AddHediff(rewardHediff);
                }
            }
            else
            {
                List<int> available = Enumerable.Range(0, BuffDefNames.Length)
                    .Except(ownedIndices).ToList();

                if (available.Count > 0)
                {
                    int chosenIndex = available.RandomElement();

                    HediffDef chosenDef = HediffDef.Named(BuffDefNames[chosenIndex]);
                    Hediff hediff = HediffMaker.MakeHediff(chosenDef, pawn);
                    hediff.Severity = 1f;
                    pawn.health.AddHediff(hediff);

                    SpawnFloatingMote(pawn, MoteDefNames[chosenIndex]);
                }
            }
        }

        private void SpawnFloatingMote(Pawn pawn, string moteDefName)
        {
            ThingDef moteDef = ThingDef.Named(moteDefName);
            if (moteDef == null) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            Vector3 pos = pawn.DrawPos;
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            mote.exactPosition = pos;
            mote.Scale = 1.5f;
            mote.SetVelocity(0f, 1.5f);
            GenSpawn.Spawn(mote, pawn.Position, pawn.Map);
        }
    }
}