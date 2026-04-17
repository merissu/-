using RimWorld;
using Verse;
using System;

namespace merissu
{
    public class CompProperties_UseEffect_TriggerRaidWithLock : CompProperties
    {
        public IncidentDef incident; 

        public CompProperties_UseEffect_TriggerRaidWithLock()
        {
            this.compClass = typeof(CompUseEffect_TriggerRaidWithLock);
        }
    }

    public class CompUseEffect_TriggerRaidWithLock : CompUseEffect
    {
        public CompProperties_UseEffect_TriggerRaidWithLock Props => (CompProperties_UseEffect_TriggerRaidWithLock)this.props;

        public override void DoEffect(Pawn usedBy)
        {
            if (Props?.incident == null)
            {
                Log.Error("merissu: IncidentDef is null in XML.");
                return;
            }

            if (usedBy?.Map == null) return;
            Map map = usedBy.Map;

            var manager = map.GetComponent<Merissu_RaidManager>();
            if (manager != null)
            {
                if (manager.unlockedLevel > 1)
                {
                    manager.unlockedLevel--;
                    Messages.Message("贫穷神的能力发动了,敌人变成杂鱼了!当前等级：" + manager.unlockedLevel,
                        MessageTypeDefOf.CautionInput);
                }
                else
                {
                    Messages.Message("已经只剩杂鱼了,难度无法继续降低",
                        MessageTypeDefOf.NeutralEvent);
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