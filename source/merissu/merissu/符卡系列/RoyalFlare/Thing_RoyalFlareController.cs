using Verse;

namespace merissu
{
    public class Thing_RoyalFlareController : Thing
    {
        private Pawn caster;
        private int ticks;

        public void Init(Pawn pawn)
        {
            caster = pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            ticks++;

            if (caster == null || caster.Destroyed)
            {
                Destroy();
                return;
            }

            if (ticks == 1)
                SpawnSun();

            if (ticks == 120)
                SpawnEmitter();

            if (ticks > 300)
                Destroy();
        }

        private void SpawnSun()
        {
            var sun = (Thing_RoyalFlareSun)ThingMaker.MakeThing(
                ThingDef.Named("RoyalFlareSun"));
            sun.Init(caster);
            GenSpawn.Spawn(sun, caster.Position, caster.Map);
        }

        private void SpawnEmitter()
        {
            var emitter = (Thing_RoyalFlareShockwaveEmitter)
                ThingMaker.MakeThing(
                    ThingDef.Named("RoyalFlareShockwaveEmitter"));
            emitter.Init(caster);
            GenSpawn.Spawn(emitter, caster.Position, caster.Map);
        }
    }
}
