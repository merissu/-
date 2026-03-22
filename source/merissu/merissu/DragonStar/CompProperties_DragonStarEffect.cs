using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class CompProperties_DragonStarEffect : CompProperties
    {
        public CompProperties_DragonStarEffect()
        {
            this.compClass = typeof(CompUseEffect_DragonStarEffect);
        }
    }

    public class CompUseEffect_DragonStarEffect : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            Map map = user.Map;
            if (map == null) return;

            FleckDef moneyEffectDef = DefDatabase<FleckDef>.GetNamed("Merissu_OvernightMoneyEffect", false);

            if (moneyEffectDef != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(user.TrueCenter(), map, moneyEffectDef);

                data.rotation = Rand.Range(0f, 360f);
                data.rotationRate = Rand.Range(-180f, 180f);
                data.scale = 1.2f;

                map.flecks.CreateFleck(data);
            }
        }
    }
}