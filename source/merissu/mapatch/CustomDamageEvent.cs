using AM.Events;
using UnityEngine;

namespace merissu.Events
{
    [CreateAssetMenu(fileName = "CustomDamage", menuName = "Events/CustomDamage")]
    public class CustomDamageEvent : EventBase
    {
        public override string EventID => "CustomDamage";

        public int KillerIndex;
        public int VictimIndex = 1;
        public string TargetBodyPart = "Torso";
        public string DamageDef = "Blunt";
        public int DamageAmount = 100;

        protected override void Expose()
        {
            Look(ref KillerIndex);
            Look(ref VictimIndex);
            Look(ref TargetBodyPart);
            Look(ref DamageDef);
            Look(ref DamageAmount);
        }
    }
}
