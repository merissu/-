using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_ThreeHeavenlyVisuals : HediffCompProperties
    {
        public HediffCompProperties_ThreeHeavenlyVisuals()
        {
            this.compClass = typeof(HediffComp_ThreeHeavenlyVisuals);
        }
    }

    public class HediffComp_ThreeHeavenlyVisuals : HediffComp
    {
        private int lastSeverityInt = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Map == null) return;

            int currentSev = Mathf.RoundToInt(parent.Severity);

            if (currentSev > lastSeverityInt)
            {
                SpawnLevelUpMote(currentSev);
                lastSeverityInt = currentSev; 
            }
            else if (currentSev < lastSeverityInt)
            {
                lastSeverityInt = currentSev;
            }
        }

        private void SpawnLevelUpMote(int level)
        {
            string moteDefName;

            switch (level)
            {
                case 1:
                    moteDefName = "Mote_LevelUpText";
                    break;
                case 2:
                    moteDefName = "Mote_LevelUpTwoDef";
                    break;
                case 3:
                    moteDefName = "Mote_invincible";
                    break;
                default:
                    return; 
            }

            ThingDef moteDef = ThingDef.Named(moteDefName);
            if (moteDef == null) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            Vector3 pos = Pawn.DrawPos;
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            mote.exactPosition = pos;
            mote.Scale = 1.3f;  
            mote.SetVelocity(0f, 1.2f); 

            GenSpawn.Spawn(mote, Pawn.Position, Pawn.Map);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastSeverityInt, "lastSeverityInt_ThreeHeavenly", 0);
        }
    }
}