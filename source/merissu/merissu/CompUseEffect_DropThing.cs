using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_UseEffect_DropThing : CompProperties_UseEffect
    {
        public ThingDef thingDef;      
        public int count = 1;          
        public bool spawnNear = true;  

        public CompProperties_UseEffect_DropThing()
        {
            this.compClass = typeof(CompUseEffect_DropThing);
        }
    }

    public class CompUseEffect_DropThing : CompUseEffect
    {
        public CompProperties_UseEffect_DropThing Props => (CompProperties_UseEffect_DropThing)this.props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (Props.thingDef == null)
            {
                Log.Error($"[merissu] {this.parent.def.defName} 的 CompUseEffect_DropThing 缺少 thingDef 定义。");
                return;
            }

            Thing thing = ThingMaker.MakeThing(Props.thingDef);
            thing.stackCount = Props.count;

            Map map = usedBy.Map;
            IntVec3 pos = usedBy.Position;

            if (GenPlace.TryPlaceThing(thing, pos, map, Props.spawnNear ? ThingPlaceMode.Near : ThingPlaceMode.Direct))
            {
            }
            else
            {
                Log.Warning($"[merissu] 无法为 {usedBy.Name} 生成掉落物 {thing.def.defName}，空间不足？");
            }
        }
    }
}