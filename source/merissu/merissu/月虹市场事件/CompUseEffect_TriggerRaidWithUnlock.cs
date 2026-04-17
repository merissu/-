using RimWorld;
using Verse;
using System;

namespace merissu
{
    public class CompProperties_UseEffect_TriggerRaidWithUnlock : CompProperties
    {
        public IncidentDef incident;

        public CompProperties_UseEffect_TriggerRaidWithUnlock()
        {
            this.compClass = typeof(CompUseEffect_TriggerRaidWithUnlock);
        }
    }

    public class CompUseEffect_TriggerRaidWithUnlock : CompUseEffect
    {
        public CompProperties_UseEffect_TriggerRaidWithUnlock Props => (CompProperties_UseEffect_TriggerRaidWithUnlock)this.props;

        public override void DoEffect(Pawn usedBy)
        {
            if (Props?.incident == null) return;
            if (usedBy?.Map == null) return;
            Map map = usedBy.Map;

            var manager = map.GetComponent<Merissu_RaidManager>();
            if (manager != null)
            {
                if (manager.unlockedLevel < 5)
                {
                    manager.unlockedLevel++;
                    Messages.Message("疫病神的能力发动了,吸引了更高阶的妖怪！当前等级：" + manager.unlockedLevel,
                        MessageTypeDefOf.ThreatBig);
                }
                else
                {
                    Messages.Message("你已经被最强大的妖怪注意到了,难度已达极限！",
                        MessageTypeDefOf.RejectInput);
                }
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(Props.incident.category, map);
            parms.target = map;
            parms.forced = true;

            if (!Props.incident.Worker.TryExecute(parms))
            {
                Messages.Message("事件 " + Props.incident.label + " 触发失败。", MessageTypeDefOf.RejectInput);
            }
        }
    }
}