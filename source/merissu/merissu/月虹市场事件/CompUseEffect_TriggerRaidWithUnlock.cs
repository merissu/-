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

                    string levelText;
                    switch (manager.unlockedLevel)
                    {
                        case 1: levelText = "<color=#00FF00>【春之小雨】</color>"; break;
                        case 2: levelText = "<color=#00BFFF>【夏之阵雨】</color>"; break;
                        case 3: levelText = "<color=#FF4500>【秋之台风】</color>"; break;
                        case 4: levelText = "<color=#FF69B4>【蛰居之冬】</color>"; break;
                        case 5: levelText = "<color=#BF00FF>【第五个季节】</color>"; break;
                        default: levelText = "<color=#BF00FF>【未知威胁】</color>"; break;
                    }

                    Messages.Message($"疫病神的能力发动了，吸引了更强大的妖怪！当前难度：{levelText}",
                        MessageTypeDefOf.ThreatBig);
                }
                else
                {
                    Messages.Message("你已经被<color=#BF00FF>最强大的妖怪</color>注意到了，难度已达极限！",
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