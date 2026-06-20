using Verse;

namespace merissu
{
    public class CompProperties_YinYangOrb : CompProperties
    {
        public float radius = 4f;
        public float damagePerTick = 200f;

        public float moveSpeed = 0.08f;

        public float rotateSpeed = 3f;

        public int lingerTicks = 120;

        public float explosionRadius = 6f;
        public int explosionDamage = 200;

        public CompProperties_YinYangOrb()
        {
            compClass = typeof(Comp_YinYangOrb);
        }
    }
}
