using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace merissu
{
    public class DeathActionWorker_YinYangJadeDie : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord lord)
        {
            if (corpse == null || corpse.Map == null) return;

            IntVec3 pos = corpse.Position;
            Map map = corpse.Map;
            Pawn pawn = corpse.InnerPawn;

            Thing anim = ThingMaker.MakeThing(ThingDef.Named("Merissu_YinYangDieAnim"));
            GenSpawn.Spawn(anim, pos, map);

            if (pawn.def.butcherProducts != null)
            {
                foreach (ThingDefCountClass prod in pawn.def.butcherProducts)
                {
                    Thing thing = ThingMaker.MakeThing(prod.thingDef);
                    thing.stackCount = prod.count;
                    GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
                }
            }

            if (!corpse.Destroyed)
            {
                corpse.Destroy(DestroyMode.Vanish);
            }
        }
    }

    public class Thing_YinYangDieAnim : Thing
    {
        private int age = 0;
        private const int TicksPerFrame = 5; 
        private const int TotalFrames = 4;   
        private float drawSize = 1.0f;

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= TicksPerFrame * TotalFrames)
            {
                this.Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = Mathf.Min(age / TicksPerFrame, TotalFrames - 1);

            Material mat = MaterialPool.MatFrom($"Pawn/die/{frame:D3}", ShaderDatabase.MoteGlow);

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 s = new Vector3(drawSize, 1f, drawSize);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, s);

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age", 0);
        }
    }
}