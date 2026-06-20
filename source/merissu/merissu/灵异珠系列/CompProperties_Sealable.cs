using RimWorld;
using Verse;
using System.Collections.Generic;

namespace merissu
{
    public class CompProperties_Sealable : CompProperties
    {
        public ThingDef targetDef; 
        public ThingDef costDef;   
        public int costCount = 1;

        public CompProperties_Sealable()
        {
            this.compClass = typeof(CompSealable);
        }
    }

    public class CompSealable : ThingComp
    {
        public CompProperties_Sealable Props => (CompProperties_Sealable)this.props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "封印灵异珠",
                defaultDesc = $"消耗一个[{Props.costDef.label}]，将庭灯永久封印，使其获得无限能源。",
                icon = Props.costDef.uiIcon, 
                action = delegate
                {
                    TrySeal();
                }
            };
        }

        private void TrySeal()
        {
            Map map = this.parent.Map;
            Faction faction = this.parent.Faction;
            IntVec3 position = this.parent.Position;
            Rot4 rotation = this.parent.Rotation;

            Thing bead = tcFindBead(map);
            if (bead == null)
            {
                Messages.Message("地图上没有可用的" + Props.costDef.label, MessageTypeDefOf.RejectInput, false);
                return;
            }

            bead.SplitOff(Props.costCount).Destroy();

            this.parent.Destroy(DestroyMode.Vanish);
            Building newBuilding = (Building)ThingMaker.MakeThing(Props.targetDef, null);
            newBuilding.SetFaction(faction, null);
            GenSpawn.Spawn(newBuilding, position, map, rotation, WipeMode.Vanish, false);

            FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, 2f);
            Messages.Message("成功将灵异珠封印入神社庭灯！", newBuilding, MessageTypeDefOf.PositiveEvent, true);
        }

        private Thing tcFindBead(Map map)
        {
            return tcFindThing(map, Props.costDef);
        }

        private Thing tcFindThing(Map map, ThingDef def)
        {
            List<Thing> list = map.listerThings.ThingsOfDef(def);
            foreach (Thing t in list)
            {
                if (!t.IsForbidden(Faction.OfPlayer)) return t;
            }
            return null;
        }
    }
}