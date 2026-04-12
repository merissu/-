using RimWorld;
using Verse;

namespace merissu
{
    public class CompProperties_UseEffect_TriggerIncident : CompProperties
    {
        public IncidentDef incident;
        public CompProperties_UseEffect_TriggerIncident()
        {
            this.compClass = typeof(CompUseEffect_TriggerIncident);
        }
    }

    public class CompUseEffect_TriggerIncident : CompUseEffect
    {
        public CompProperties_UseEffect_TriggerIncident Props => (CompProperties_UseEffect_TriggerIncident)this.props;

        public override void DoEffect(Pawn usedBy)
        {
            
            if (Props?.incident == null)
            {
                Log.Error("merissu: IncidentDef is null in XML config.");
                return;
            }

            if (usedBy?.Map == null) return;

            Map map = usedBy.Map;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(Props.incident.category, map);
            parms.target = map;
            parms.forced = true; 

            if (!Props.incident.Worker.TryExecute(parms))
            {
                Messages.Message("事件 " + Props.incident.label + " 触发失败（可能当前环境不满足条件）。", MessageTypeDefOf.RejectInput);
            }
        }
    }
}