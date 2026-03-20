using Verse;
using RimWorld;

namespace merissu
{
    public class CompGoheiFlight : ThingComp
    {
        public bool FlightEnabled;

        public Pawn Wearer => parent is ThingWithComps t && t.ParentHolder is Pawn_EquipmentTracker eq
            ? eq.pawn
            : null;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref FlightEnabled, "FlightEnabled", false);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            // 装备时默认不飞
            FlightEnabled = false;
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            // 卸下时强制关闭飞行
            FlightEnabled = false;
        }
    }
}
