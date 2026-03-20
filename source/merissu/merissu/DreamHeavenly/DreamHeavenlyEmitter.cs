using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    public class DreamHeavenlyEmitter : Thing
    {
        private Pawn caster;
        private int age;
        private int duration;
        private bool finished;

        private List<SmallYinYangOrb> orbs = new List<SmallYinYangOrb>();

        private const int FireInterval = 1;
        private const float RotateSpeed = 3f;

        public void Initialize(Pawn pawn, int durationTicks)
        {
            caster = pawn;
            duration = durationTicks;
            SpawnYinYangOrbs();
        }

        private void SpawnYinYangOrbs()
        {
            for (int i = 0; i < 7; i++)
            {
                SmallYinYangOrb orb =
                    (SmallYinYangOrb)ThingMaker.MakeThing(
                        ThingDef.Named("SmallYinYangOrb"));

                orb.caster = caster;
                orb.parentEmitter = this;
                orb.angleOffset = i * (360f / 7f);

                GenSpawn.Spawn(orb, caster.Position, caster.Map);
                orbs.Add(orb);
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (finished)
                return;

            age++;

            if (caster == null || caster.Dead)
            {
                End();
                return;
            }

            Position = caster.Position;

            if (age % 20 == 0)
            {
                RingShockwave wave =
                    (RingShockwave)ThingMaker.MakeThing(
                        ThingDef.Named("RingShockwave"));

                wave.caster = caster;
                GenSpawn.Spawn(wave, caster.Position, caster.Map);
            }

            if (age % FireInterval == 0)
            {
                FireBullets();
            }

            if (age >= duration)
            {
                End();
            }
        }

        private void End()
        {
            if (finished)
                return;

            finished = true;

            for (int i = orbs.Count - 1; i >= 0; i--)
            {
                if (orbs[i] != null && !orbs[i].Destroyed)
                    orbs[i].Destroy();
            }

            orbs.Clear();

            if (caster != null && caster.health != null)
            {
                Hediff inv =
                    caster.health.hediffSet.GetFirstHediffOfDef(
                        DefDatabase<HediffDef>.GetNamedSilentFail(
                            "DreamHeavenlyInvincible"));

                if (inv != null)
                    caster.health.RemoveHediff(inv);
            }

            if (!Destroyed)
                Destroy();
        }

        private void FireBullets()
        {
            for (int i = 0; i < 4; i++)
            {
                float baseAngle = i * 90f;

                FireOne(baseAngle + age * RotateSpeed);
                FireOne(baseAngle - age * RotateSpeed);
            }
        }

        private void FireOne(float angle)
        {
            Vector3 dir =
                Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            Vector3 target =
                caster.DrawPos + dir * 40f;

            Projectile proj =
                (Projectile)ThingMaker.MakeThing(
                    ThingDef.Named("DreamHeavenlyHakureiTalisman"));

            GenSpawn.Spawn(proj, caster.Position, caster.Map);

            proj.Launch(
                caster,
                caster.DrawPos,
                target.ToIntVec3(),
                target.ToIntVec3(),
                ProjectileHitFlags.All);
        }
    }
}
