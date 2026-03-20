using Verse;
using RimWorld;
using Verse.Sound; 

namespace merissu
{
    public class Thing_LunaticRedEyesEmitter : Thing
    {
        private Pawn caster;
        private int age;

        private const int Duration = 360;
        private const int WaveInterval = 8;

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Dead || Map == null)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            if (age % WaveInterval == 0)
            {
                SpawnWave();
            }

            if (age > Duration)
            {
                Destroy();
            }
        }

        private void SpawnWave()
        {
            Thing_LunaticRedEyesWave wave = (Thing_LunaticRedEyesWave)ThingMaker.MakeThing(
                ThingDef.Named("LunaticRedEyesWave"));

            wave.Init(caster);
            GenSpawn.Spawn(wave, caster.Position, Map);

            SoundDef sound = SoundDef.Named("CorollaVision");
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(caster.Position, Map));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref age, "age", 0);
        }
    }
}