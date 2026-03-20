using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Thing_NoachianDelugeEmitter : Thing
    {
        private Pawn caster;
        private LocalTargetInfo targetInfo; 
        private Vector3 lastTargetPos;     
        private int ticksLeft = 300;
        private int fireInterval = 1;
        private int fireTick;

        private float waveFrequency = 0.15f;
        private float waveAmplitude = 12f;

        public void Init(Pawn caster, LocalTargetInfo target)
        {
            this.caster = caster;
            this.targetInfo = target;
            this.lastTargetPos = target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted();
        }

        protected override void Tick()
        {
            if (caster == null || !caster.Spawned || caster.Map == null)
            {
                if (!Destroyed) Destroy();
                return;
            }

            if (targetInfo.HasThing && targetInfo.Thing.Spawned)
            {
                lastTargetPos = targetInfo.Thing.DrawPos;
            }

            ticksLeft--;
            fireTick++;

            if (fireTick >= fireInterval)
            {
                fireTick = 0;
                FireOnce();
            }

            if (ticksLeft <= 0)
            {
                Destroy();
            }
        }

        private void FireOnce()
        {
            Vector3 origin = caster.DrawPos;
            int ticksPassed = 300 - ticksLeft; 

            Vector3 baseDir = (lastTargetPos - origin).normalized;
            if (baseDir == Vector3.zero) baseDir = caster.Rotation.FacingCell.ToVector3();

            float[] phaseOffsets = { 0f, 0f, 2.0f, -2.0f, 4.0f, -4.0f };
            float[] angleBias = { -8f, 8f, -4f, 4f, -12f, 12f };

            for (int i = 0; i < 6; i++)
            {
                float waveAngle = Mathf.Sin((ticksPassed + phaseOffsets[i] * 10f) * waveFrequency) * waveAmplitude;
                float finalAngle = waveAngle + angleBias[i];

                Vector3 rotatedDir = Quaternion.Euler(0, finalAngle, 0) * baseDir;

                float maxDistance = 50f;
                Vector3 infiniteTargetPos = origin + rotatedDir * maxDistance;

                Projectile proj = (Projectile)GenSpawn.Spawn(
                    ThingDef.Named("Projectile_NoachianDeluge"),
                    caster.Position, 
                    caster.Map);

                proj.Launch(
                    caster,
                    origin,
                    new LocalTargetInfo(infiniteTargetPos.ToIntVec3()),
                    targetInfo.Thing, 
                    ProjectileHitFlags.All,
                    false,
                    null,
                    null
                );
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