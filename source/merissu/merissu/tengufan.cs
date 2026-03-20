using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class CompProperties_TenguFanMote : CompProperties
    {
        public ThingDef moteDef;
        public Vector3 offset = new Vector3(0, 0, -0.5f); 
        public Vector2 customScale = new Vector2(2f, 1f); 

        public CompProperties_TenguFanMote()
        {
            this.compClass = typeof(Comp_TenguFanMote);
        }
    }

    public class Comp_TenguFanMote : ThingComp
    {
        public override void PostIngested(Pawn ingester)
        {
            if (ingester == null || !ingester.Spawned || ingester.Map == null) return;

            var props = (CompProperties_TenguFanMote)this.props;
            if (props.moteDef != null)
            {
                Mote mote = (Mote)ThingMaker.MakeThing(props.moteDef);
                if (mote != null)
                {
                    mote.Attach(ingester);

                    mote.exactPosition = ingester.DrawPos + props.offset;


                    GenSpawn.Spawn(mote, ingester.Position, ingester.Map);
                }
            }
        }
    }
}