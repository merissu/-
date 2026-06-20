using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound; 

namespace merissu
{
    public class Thing_EternalMeekEmitter : Thing
    {
        private Pawn caster;
        private LocalTargetInfo targetInfo;
        private Vector3 lastTargetPos;
        private int ticksLeft = 300; 
        private int fireTick;

        private const float SpreadAngle = 60f; 
        private const int FireInterval = 1;    
        private const int BulletsPerTick = 3;  

        public void Init(Pawn caster, LocalTargetInfo target)
        {
            this.caster = caster;
            this.targetInfo = target;
            this.lastTargetPos = target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted();
        }

        protected override void Tick()
        {
            if (base.Map == null) return;

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                if (!Destroyed) this.Destroy();
                return;
            }

            if (targetInfo.HasThing && targetInfo.Thing.Spawned)
            {
                lastTargetPos = targetInfo.Thing.DrawPos;
            }
            else if (targetInfo.IsValid)
            {
                lastTargetPos = targetInfo.Cell.ToVector3Shifted();
            }

            fireTick++;
            if (fireTick >= FireInterval)
            {
                fireTick = 0;
                FireOnce();
            }

            ticksLeft--;
            if (ticksLeft <= 0)
            {
                if (!Destroyed) this.Destroy();
            }
        }

        private void FireOnce()
        {
            Vector3 origin = caster.DrawPos;

            Vector3 direction = (lastTargetPos - origin).normalized;

            if (direction == Vector3.zero)
            {
                direction = caster.Rotation.FacingCell.ToVector3();
            }

            SoundDef throwSound = SoundDef.Named("knife");

            for (int i = 0; i < BulletsPerTick; i++)
            {
                float angle = Rand.Range(-SpreadAngle / 2f, SpreadAngle / 2f);
                Vector3 shotDir = Quaternion.Euler(0, angle, 0) * direction;

                Vector3 finalTargetPos = origin + shotDir * 50f;

                Projectile proj = (Projectile)GenSpawn.Spawn(ThingDef.Named("knife"), caster.Position, caster.Map);

                proj.Launch(
                    caster,
                    origin,
                    new LocalTargetInfo(finalTargetPos.ToIntVec3()), 
                    targetInfo.Thing,                                
                    ProjectileHitFlags.All,
                    false,
                    null,
                    null);

                if (i == 0 && throwSound != null)
                {
                    throwSound.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_TargetInfo.Look(ref targetInfo, "targetInfo");
            Scribe_Values.Look(ref lastTargetPos, "lastTargetPos");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 300);
            Scribe_Values.Look(ref fireTick, "fireTick", 0);
        }
    }
}