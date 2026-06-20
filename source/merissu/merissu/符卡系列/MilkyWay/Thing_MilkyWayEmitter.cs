using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Thing_MilkyWayEmitter : Thing
    {
        private Pawn caster;

        private int ageAB;
        private int waveIndexAB;

        private const int TicksPerWaveAB = 5;
        private const int TotalWavesAB = 128;
        private const float AngleStepAB = 10f;
        private static readonly SoundDef Sound_AB_Fire =
             SoundDef.Named("MilkyWay_AB_Fire");
        private int lastABSoundTick = -999999;
        private const int AB_SoundCooldownTicks = 30; 

        private static readonly ThingDef ProjectileA =
            ThingDef.Named("MilkyWayProjectileA");
        private static readonly ThingDef ProjectileB =
            ThingDef.Named("MilkyWayProjectileB");

        private int ageCD;
        private int waveIndexCD;

        private const int TicksPerWaveCD = 10;
        private const int TotalWavesCD = 64;

        private const float AngleStepCD = 10f;
        private const int RowsPerWaveCD = 4;
        private const float RowAngleStepCD = 4f;

        private static readonly ThingDef ProjectileC =
            ThingDef.Named("MilkyWayProjectileC");
        private static readonly ThingDef ProjectileD =
            ThingDef.Named("MilkyWayProjectileD");

        private const float VirtualRange = 99f;

        public void Init(Pawn pawn)
        {
            caster = pawn;

            ageAB = 0;
            waveIndexAB = 0;

            ageCD = 0;
            waveIndexCD = 0;
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || caster.Map == null)
            {
                Destroy();
                return;
            }

            TickAB();
            TickCD();

            if (waveIndexAB >= TotalWavesAB &&
                waveIndexCD >= TotalWavesCD)
            {
                Destroy();
            }
        }

        private void TickAB()
        {
            if (waveIndexAB >= TotalWavesAB) return;

            ageAB++;

            if (ageAB % TicksPerWaveAB != 0)
                return;

            FireWaveAB();
            waveIndexAB++;
        }

        private void FireWaveAB()
        {
            int now = Find.TickManager.TicksGame;

            if (now - lastABSoundTick >= AB_SoundCooldownTicks)
            {
                Sound_AB_Fire?.PlayOneShot(
                    new TargetInfo(caster.Position, caster.Map)
                );

                lastABSoundTick = now;
            }

            bool useA = (waveIndexAB % 2 == 0);
            ThingDef projectileDef = useA ? ProjectileA : ProjectileB;

            Vector3 origin = caster.DrawPos;
            float baseAngle = waveIndexAB * AngleStepAB;

            for (int i = 0; i < 8; i++)
            {
                float angle = baseAngle + i * 45f;

                Vector3 dir =
                    Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                Vector3 dest = origin + dir * VirtualRange;

                Projectile p =
                    (Projectile)ThingMaker.MakeThing(projectileDef);

                GenSpawn.Spawn(p, origin.ToIntVec3(), caster.Map);

                p.Launch(
                    caster,
                    origin,
                    new LocalTargetInfo(dest.ToIntVec3()),
                    new LocalTargetInfo(dest.ToIntVec3()),
                    ProjectileHitFlags.IntendedTarget,
                    false
                );
            }
        }

        private void TickCD()
        {
            if (waveIndexCD >= TotalWavesCD) return;

            ageCD++;

            if (ageCD % TicksPerWaveCD != 0)
                return;

            FireWaveCD();
            waveIndexCD++;
        }

        private void FireWaveCD()
        {
            bool useC = (waveIndexCD % 2 == 0);
            ThingDef projectileDef = useC ? ProjectileC : ProjectileD;

            Vector3 origin = caster.DrawPos;
            float baseAngle = waveIndexCD * AngleStepCD;

            for (int row = 0; row < RowsPerWaveCD; row++)
            {
                float rowAngle = baseAngle + row * RowAngleStepCD;

                for (int i = 0; i < 8; i++)
                {
                    float angle = rowAngle + i * 45f;

                    Vector3 dir =
                        Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                    Vector3 dest = origin + dir * VirtualRange;

                    Projectile p =
                        (Projectile)ThingMaker.MakeThing(projectileDef);

                    GenSpawn.Spawn(p, origin.ToIntVec3(), caster.Map);

                    p.Launch(
                        caster,
                        origin,
                        new LocalTargetInfo(dest.ToIntVec3()),
                        new LocalTargetInfo(dest.ToIntVec3()),
                        ProjectileHitFlags.IntendedTarget,
                        false
                    );
                }
            }
        }
    }
}
