using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_GoheiDelayedOrbit : GoheiAttackMode
    {
        public override string ModeName => "GoheiDelayedOrbit";
        protected override string ProjectileDefName => "REIMU_SpinningTalisman"; 
        protected override string SoundDefName => "ReimuTalismanA";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1.2f;

        private const string ConvertSoundName = "ReimuTalismanB";
        private const int FakeProjectileCount = 6;
        private const float SpawnDistance = 2.5f;
        private const float ArcRadius = 1.8f;

        private const float MoveDuration = 0.3f;
        private const float IdleDuration = 0.7f;

        public override bool OverrideCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            SoundDef.Named(SoundDefName)?.PlayOneShot(new TargetInfo(caster.Position, map));

            Vector3 aimDir;
            if (target.IsValid && target.Cell != caster.Position)
                aimDir = (target.Cell.ToVector3Shifted() - caster.DrawPos).normalized;
            else
                aimDir = caster.Rotation.FacingCell.ToVector3().normalized;

            Vector3 centerPos = caster.DrawPos + aimDir * SpawnDistance;
            float baseAngle = aimDir.AngleFlat();
            float arcStart = baseAngle - 90f;
            float arcStep = 180f / (FakeProjectileCount - 1);

            Vector3 startPos = caster.DrawPos; 
            int moveTicks = (int)(MoveDuration * 60f);
            int idleTicks = (int)(IdleDuration * 60f);

            for (int i = 0; i < FakeProjectileCount; i++)
            {
                float angle = arcStart + arcStep * i;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * ArcRadius;
                Vector3 endPos = centerPos + offset;
                IntVec3 cell = endPos.ToIntVec3();

                ThingDef moteDef = ThingDef.Named("Mote_REIMU_DelayedTalisman");
                Mote_REIMU_DelayedTalisman mote = (Mote_REIMU_DelayedTalisman)ThingMaker.MakeThing(moteDef);
                mote.startPosition = startPos;
                mote.targetPosition = endPos;
                mote.moveDurationTicks = moveTicks;
                mote.idleDurationTicks = idleTicks;
                mote.targetToFireAt = target;
                mote.launcher = caster;
                mote.convertSound = SoundDef.Named(ConvertSoundName);
                GenSpawn.Spawn(mote, cell, map);
            }

            return true;
        }
    }
}