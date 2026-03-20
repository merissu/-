using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_ResurrectionFan : HediffCompProperties
    {
        public ThingDef moteDef;

        public HediffCompProperties_ResurrectionFan()
        {
            this.compClass = typeof(HediffComp_ResurrectionFan);
        }
    }

    public class HediffComp_ResurrectionFan : HediffComp
    {
        private Mote fanMote;

        public HediffCompProperties_ResurrectionFan Props =>
            (HediffCompProperties_ResurrectionFan)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (!Pawn.Spawned || Pawn.Dead)
                return;

            if (fanMote == null || fanMote.Destroyed)
            {
                SpawnFan();
            }
            else
            {
                if (fanMote.Position != Pawn.Position)
                {
                    fanMote.Position = Pawn.Position;
                }
                fanMote.exactPosition = Pawn.DrawPos;
            }
        }
        private void SpawnFan()
        {
            if (Props.moteDef == null || Pawn.Map == null)
                return;

            fanMote = (Mote)ThingMaker.MakeThing(Props.moteDef);

            fanMote.Attach(Pawn);
            fanMote.exactPosition = Pawn.DrawPos;

            GenSpawn.Spawn(fanMote, Pawn.Position, Pawn.Map);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (fanMote != null && !fanMote.Destroyed)
            {
                fanMote.Destroy();
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref fanMote, "fanMote");
        }
    }

    public class Graphic_ResurrectionFan : Graphic_Single
    {
        public override void DrawWorker(
            Vector3 loc,
            Rot4 rot,
            ThingDef thingDef,
            Thing thing,
            float extraRotation)
        {
            if (!(thing is Mote mote))
                return;

            Vector3 drawLoc = loc;

            if (mote.link1.Linked && mote.link1.Target.HasThing)
            {
                Pawn pawn = mote.link1.Target.Thing as Pawn;
                if (pawn == null)
                    return;

                drawLoc = pawn.DrawPos;

                drawLoc += new Vector3(0f, 0f, -0.4f);

                float t = Find.TickManager.TicksGame * 0.05f;
                drawLoc.z += Mathf.Sin(t) * 0.15f;

                drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            }

            float alpha =
                0.6f + Mathf.Sin(Find.TickManager.TicksGame * 0.08f) * 0.25f;

            Color color = Color.white;
            color.a = Mathf.Clamp01(alpha);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(
                    drawLoc,
                    Quaternion.identity, 
                    new Vector3(drawSize.x, 1f, drawSize.y)
                ),
                MatSingle,
                0,
                null,
                0,
                block
            );
        }
    }
}
