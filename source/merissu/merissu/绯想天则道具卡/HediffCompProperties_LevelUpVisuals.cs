using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_LevelUpVisuals : HediffCompProperties
    {
        public HediffCompProperties_LevelUpVisuals()
        {
            this.compClass = typeof(HediffComp_LevelUpVisuals);
        }
    }

    public class HediffComp_LevelUpVisuals : HediffComp
    {
        private float lastSeverity = -1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Map == null) return;

            if (parent.Severity > lastSeverity + 0.1f)
            {
                SpawnRisingTextMote();
                lastSeverity = parent.Severity;
            }

            if (parent.Severity < lastSeverity)
            {
                lastSeverity = parent.Severity;
            }
        }

        private void SpawnRisingTextMote()
        {
            string moteDefName = "Mote_LevelUpText";
            float s = parent.Severity;

            if (s >= 0.90f)
            {
                moteDefName = "Mote_LevelUpMaxDef";
            }
            else if (s >= 0.70f)
            {
                moteDefName = "Mote_LevelUpThreeDef";
            }
            else if (s >= 0.45f) 
            {
                moteDefName = "Mote_LevelUpTwoDef";
            }

            ThingDef moteDef = ThingDef.Named(moteDefName);
            if (moteDef == null) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            Vector3 pos = Pawn.DrawPos;
            pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            mote.exactPosition = pos;
            mote.Scale = 1.0f;
            mote.SetVelocity(0f, 1.5f);

            GenSpawn.Spawn(mote, Pawn.Position, Pawn.Map);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastSeverity, "lastSeverity", -1f);
        }
    }
}