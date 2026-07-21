using UnityEngine;
using Verse;

namespace merissu
{
    public class AttackMode_WaveGunBurst5 : WaveGunAttackMode
    {
        public override string ModeName => "WaveGunBurst5";
        protected override string ProjectileDefName => "MindStarMine";
        protected override string SoundDefName => "udongegun";

        public override int BurstCount => 5;
        public override int TicksBetweenShots => 5;      
        public override float WarmupTime => 1f;        
        public override bool PlaySoundOnEveryShot => true; 
    }
    public class CompProperties_UdongeDeathBurst : CompProperties
    {
        public ThingDef particleMoteDef;
        public int particleCount = 10;     
        public float minSpeed = 8f;     
        public float maxSpeed = 10f;     
        public float minScale = 0.01f;
        public float maxScale = 0.2f;     

        public CompProperties_UdongeDeathBurst()
        {
            this.compClass = typeof(Comp_UdongeDeathBurst);
        }
    }

    public class Comp_UdongeDeathBurst : ThingComp
    {
        public CompProperties_UdongeDeathBurst Props => (CompProperties_UdongeDeathBurst)props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap == null) return;

            ThingDef moteDef = Props.particleMoteDef ?? ThingDef.Named("Mote_UdongeParticle");
            Vector3 spawnPos = parent.DrawPos;

            for (int i = 0; i < Props.particleCount; i++)
            {
                float angle = Rand.Range(0f, 360f);
                float speed = Rand.Range(Props.minSpeed, Props.maxSpeed);

                MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
                mote.exactPosition = spawnPos;
                mote.exactRotation = Rand.Range(0f, 360f);
                mote.Scale = Rand.Range(Props.minScale, Props.maxScale);
                mote.SetVelocity(angle, speed); 
                mote.rotationRate = Rand.Range(-180f, 180f); 

                GenSpawn.Spawn(mote, spawnPos.ToIntVec3(), previousMap);
            }
        }
    }
}