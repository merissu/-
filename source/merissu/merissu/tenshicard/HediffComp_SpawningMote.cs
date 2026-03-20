using Verse;
using RimWorld;
using UnityEngine;

namespace merissu
{
    public class HediffCompProperties_SpawningMote : HediffCompProperties
    {
        public ThingDef moteDef;
        public int intervalTicks = 300;

        public HediffCompProperties_SpawningMote()
        {
            this.compClass = typeof(HediffComp_SpawningMote);
        }
    }

    public class HediffComp_SpawningMote : HediffComp
    {
        private int ticksUntilNextMote;
        public HediffCompProperties_SpawningMote Props => (HediffCompProperties_SpawningMote)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn.Spawned && !Pawn.Dead)
            {
                ticksUntilNextMote--;
                if (ticksUntilNextMote <= 0)
                {
                    SpawnAnimation();
                    ticksUntilNextMote = Props.intervalTicks;
                }
            }
        }

        private void SpawnAnimation()
        {
            if (Props.moteDef != null && Pawn.Spawned && Pawn.Map != null)
            {
                Mote mote = (Mote)ThingMaker.MakeThing(Props.moteDef);
                if (mote != null)
                {
                    mote.Attach(Pawn);
                    Vector3 offset = new Vector3(0f, 0f, 0f);
                    mote.exactPosition = Pawn.DrawPos + offset;
                    GenSpawn.Spawn(mote, Pawn.Position, Pawn.Map);
                }
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksUntilNextMote, "ticksUntilNextMote", 0);
        }
    }

    public class Graphic_AnimatedMote : Graphic_Collection
    {
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (subGraphics == null || subGraphics.Length == 0) return;

            Vector3 drawLoc = loc;
            if (thing is Mote attachedMote && attachedMote.link1.Linked && attachedMote.link1.Target.HasThing)
            {
                drawLoc = attachedMote.link1.Target.Thing.DrawPos;
                drawLoc.y = loc.y; 
            }

            int ticksPerFrame = 2;
            int index = (Find.TickManager.TicksGame / ticksPerFrame) % subGraphics.Length;

            subGraphics[index].DrawWorker(drawLoc, rot, thingDef, thing, extraRotation);
        }
    }
}