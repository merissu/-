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

                    string levelText;
                    switch (manager.unlockedLevel)
                    {
                        case 1: levelText = "<color=#00FF00>【春之小雨】</color>"; break;
                        case 2: levelText = "<color=#00BFFF>【夏之阵雨】</color>"; break;
                        case 3: levelText = "<color=#FF4500>【秋之台风】</color>"; break;
                        case 4: levelText = "<color=#FF69B4>【蛰居之冬】</color>"; break;
                        default: levelText = "<color=#BF00FF>【第五个季节】</color>"; break;
                    }

                    Messages.Message($"贫穷神的能力发动了，敌人变弱了！当前难度：{levelText}",
                        MessageTypeDefOf.CautionInput);
                }
                else
                {
                    Messages.Message("已经只剩<color=#00FF00>杂鱼</color>了，难度无法继续降低。",
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