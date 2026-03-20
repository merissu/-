using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_MadBerserkExplosion : HediffCompProperties
    {
        public float explosionRadius;
        public int damageAmount;
        public DamageDef damageDef;
        public float berserkHours;
        public SongDef songDef;

        public HediffCompProperties_MadBerserkExplosion()
        {
            compClass = typeof(HediffComp_MadBerserkExplosion);
        }
    }
}
