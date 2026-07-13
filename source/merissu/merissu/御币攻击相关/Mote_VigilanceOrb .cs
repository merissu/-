using UnityEngine;
using Verse;

namespace merissu
{
    public class Mote_VigilanceOrb : Thing
    {
        public Vector3 startPos, endPos;
        public int durationTicks = 18; 
        private int age;
        public Thing launcher;
        public SoundDef impactSound;

        public override Vector3 DrawPos => Vector3.Lerp(startPos, endPos, (float)age / durationTicks);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            age = 0;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= durationTicks)
            {
                SpawnShockwave();
                Destroy();
            }
        }

        private void SpawnShockwave()
        {
            if (Map == null) return;
            ThingDef def = ThingDef.Named("Mote_VigilanceShockwave");
            Mote_VigilanceShockwave shock = (Mote_VigilanceShockwave)ThingMaker.MakeThing(def);
            shock.Position = DrawPos.ToIntVec3();
            shock.exactPosition = DrawPos;
            shock.rotationAngle = (endPos - startPos).AngleFlat();
            GenSpawn.Spawn(shock, shock.Position, Map);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 pos = DrawPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float angle = (endPos - startPos).AngleFlat();
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            Vector3 scale = new Vector3(1f, 1f, 1f);
            Material mat = def.graphicData.Graphic.MatSingle;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}