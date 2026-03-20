using System.Collections.Generic;
using Verse;

namespace merissu
{
    public class Thing_SagesStoneOrbitController : Thing
    {
        private Pawn caster;
        private int age;

        private List<Thing_SagesStone> stones =
            new List<Thing_SagesStone>();

        public const int Duration = 7500;

        public void Init(Pawn pawn)
        {
            caster = pawn;

            string[] defs =
            {
                "SagesStone_Fire",
                "SagesStone_Water",
                "SagesStone_Wood",
                "SagesStone_Metal",
                "SagesStone_Earth"
            };

            for (int i = 0; i < defs.Length; i++)
            {
                Thing_SagesStone stone =
                    ThingMaker.MakeThing(
                        ThingDef.Named(defs[i]))
                    as Thing_SagesStone;

                if (stone == null) continue;

                stone.Init(caster, i, defs.Length);
                GenSpawn.Spawn(stone, caster.Position, caster.Map);
                stones.Add(stone);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed)
            {
                DestroyAll();
                return;
            }

            if (age > Duration)
            {
                DestroyAll();
            }
        }

        private void DestroyAll()
        {
            for (int i = 0; i < stones.Count; i++)
            {
                if (stones[i] != null && !stones[i].Destroyed)
                {
                    stones[i].Destroy();
                }
            }
            Destroy();
        }
    }
}
