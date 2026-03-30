using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class HediffCompProperties_MagicPotionVisuals : HediffCompProperties
    {
        public HediffCompProperties_MagicPotionVisuals()
        {
            this.compClass = typeof(HediffComp_MagicPotionVisuals);
        }
    }

    public class HediffComp_MagicPotionVisuals : HediffComp
    {
        private const int SPAWN_INTERVAL = 15;
        private Mote auraMote;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!Pawn.Spawned || Pawn.Map == null) return;

            if (Pawn.IsHashIntervalTick(SPAWN_INTERVAL))
            {
                SpawnRisingMote();
            }

            UpdateAura();
        }

        private void UpdateAura()
        {
            if (auraMote == null || auraMote.Destroyed || !auraMote.Spawned)
            {
                ThingDef auraDef = ThingDef.Named("Mote_MagicPotionAura");
                if (auraDef != null)
                {
                    auraMote = (Mote)ThingMaker.MakeThing(auraDef);
                    GenSpawn.Spawn(auraMote, Pawn.Position, Pawn.Map);
                }
                return;
            }

            Vector3 pos = Pawn.DrawPos;
            pos.z -= 0.5f;
            pos.y = 21f;
            auraMote.exactPosition = pos;

            auraMote.Maintain();
        }

        private void SpawnRisingMote()
        {
            ThingDef moteDef = ThingDef.Named("Mote_MagicPotionRising");
            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            Vector3 pos = Pawn.DrawPos;
            pos.z -= 0.4f;
            pos.y = 20f;
            mote.exactPosition = pos;
            mote.Scale = 1.2f;
            mote.SetVelocity(0f, 3.5f);
            GenSpawn.Spawn(mote, Pawn.Position, Pawn.Map);
            mote.exactPosition = new Vector3(pos.x, 20f, pos.z);
        }
    }
}