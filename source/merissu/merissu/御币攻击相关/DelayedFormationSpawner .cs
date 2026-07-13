using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class DelayedFormationSpawner : Thing
    {
        public Vector3 center;
        public Vector3 aimDirection;
        public Faction faction;
        public int delayTicks = 20; 
        private int age;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            age = 0;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= delayTicks)
            {
                ThingDef formationDef = ThingDef.Named("VigilanceFormation");
                Thing_VigilanceFormation formation = (Thing_VigilanceFormation)ThingMaker.MakeThing(formationDef);
                formation.exactPosition = center;
                formation.aimDirection = aimDirection;
                formation.faction = faction;
                GenSpawn.Spawn(formation, center.ToIntVec3(), Map);
                Destroy();
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }
    }
}