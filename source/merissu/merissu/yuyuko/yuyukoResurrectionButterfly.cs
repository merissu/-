using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class yuyukoResurrectionButterfly : Ability
    {
        private static readonly HediffDef ResurrectionDef =
            HediffDef.Named("ResurrectionButterfly");

        private static readonly HediffDef FullPowerDef =
            HediffDef.Named("FullPower");
        public yuyukoResurrectionButterfly() : base() { }

        public yuyukoResurrectionButterfly(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff res =
                    pawn.health.hediffSet.GetFirstHediffOfDef(ResurrectionDef);

                Hediff full =
                    pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

                float resSeverity = res?.Severity ?? 0f;
                float fullSeverity = full?.Severity ?? 0f;

                if (resSeverity >= 5f)
                    return AcceptanceReport.WasAccepted;

                if (resSeverity <= 4f && fullSeverity >= 1f)
                    return AcceptanceReport.WasAccepted;

                return "灵力不足";
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn.Map == null)
                return false;

            SoundDef.Named("yuyukolazer").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            pawn.pather.StopDead();
            if (pawn.jobs != null)
            {
                pawn.jobs.StopAll();
                Job waitJob = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, 300);
                pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
            }
            pawn.stances.SetStance(new Stance_ResurrectionButterfly(300, target, null));

            Hediff res =
                pawn.health.hediffSet.GetFirstHediffOfDef(ResurrectionDef);

            Hediff full =
                pawn.health.hediffSet.GetFirstHediffOfDef(FullPowerDef);

            float resSeverity = res?.Severity ?? 0f;

            if (resSeverity < 5f)
            {
                if (full != null)
                    full.Severity -= 1f;

                if (res == null)
                    res = pawn.health.AddHediff(ResurrectionDef);

                res.Severity += 1f;
            }

            int stage = Mathf.RoundToInt(res?.Severity ?? 1f);
            SpawnOrbs(stage);

            return base.Activate(target, dest);
        }

        private void SpawnOrbs(int stage)
        {
            int countPerColor = 3 + (stage - 1);
            int total = countPerColor * 2;

            for (int i = 0; i < total; i++)
            {
                Thing thing = ThingMaker.MakeThing(
                    ThingDef.Named("ResurrectionButterflyOrb"));

                if (thing is Thing_ResurrectionButterflyOrb orb)
                {
                    orb.caster = pawn;
                    orb.index = i;
                    orb.totalCount = total;
                    orb.isPink = (i % 2 == 0);
                    orb.stage = stage;

                    orb.fireOffsetTick = i % 2 == 0 ? 0 : 2;
                }

                GenSpawn.Spawn(thing, pawn.Position, pawn.Map);
            }
        }
    }

    public class Thing_ResurrectionButterflyOrb : Thing
    {
        public Pawn caster;
        public int index;
        public int totalCount;
        public bool isPink;
        public int stage;

        private int age;

        private const int Lifetime = 300;
        private const int FadeTicks = 60;

        private const float Radius = 2.5f;
        private const float MaxSize = 1.2f;
        private const float BaseLaserLength = 10f;
        private const float LaserWidth = 1f;

        private const int DamageInterval = 4;
        private const float DamageAmount = 20f;

        private int firedRows;
        private int rows;
        private int bulletsPerRow;
        private float rowInterval;
        private bool barrageInitialized;

        public int fireOffsetTick;

        private int firedFinalRings = 0;

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned || caster.Dead)
            {
                Destroy();
                return;
            }

            Position = caster.Position;
            caster.pather.StopDead();

            if (age % DamageInterval == 0)
                DoLaserDamage();

            HandleBarrage();

            if (index == 0)
            {
                HandleFinalPhase();
            }

            if (age > Lifetime)
                Destroy();
        }

        private void HandleBarrage()
        {
            if (stage < 1) return;
            if (age < 60) return;

            if (!barrageInitialized)
            {
                rows = 3 + (stage - 1);
                bulletsPerRow = 6 + (stage - 1) * 2;

                if (rows <= 1)
                    rowInterval = 180f;
                else
                    rowInterval = 180f / (rows - 1);

                barrageInitialized = true;
            }

            if (firedRows >= rows) return;

            float expectedTick = 60f + rowInterval * firedRows + fireOffsetTick;
            if (age >= Mathf.RoundToInt(expectedTick))
            {
                FireOneRow();
                firedRows++;
            }
        }

        private void HandleFinalPhase()
        {
            int finalDuration = 60;
            int startTick = Lifetime - finalDuration;

            if (age < startTick) return;

            int totalRings = stage;
            if (firedFinalRings >= totalRings) return;

            float ringInterval = (float)finalDuration / totalRings;
            float nextRingTick = startTick + (firedFinalRings * ringInterval);

            if (age >= Mathf.RoundToInt(nextRingTick))
            {
                FireFinalRing();
                firedFinalRings++;
            }
        }

        private void FireFinalRing()
        {
            SoundDef.Named("MilkyWay_AB_Fire").PlayOneShot(new TargetInfo(caster.Position, caster.Map));

            int bulletsInRing = 6 + (stage - 1) * 2;
            float angleStep = 360f / bulletsInRing;
            Vector3 center = caster.DrawPos;

            for (int i = 0; i < bulletsInRing; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                IntVec3 targetCell = (center + dir * 30f).ToIntVec3();

                Projectile proj = (Projectile)GenSpawn.Spawn(
                    ThingDef.Named("Butterfly_dayu"),
                    caster.Position,
                    Map);

                proj.Launch(
                    caster,
                    center,
                    new LocalTargetInfo(targetCell),
                    new LocalTargetInfo(targetCell),
                    ProjectileHitFlags.All,
                    false,
                    null);
            }
        }

        private void FireOneRow()
        {
            SoundDef.Named("yuyukoResurrectionButterfly").PlayOneShot(new TargetInfo(this.Position, Map));

            float baseAngle = (360f / totalCount) * index + age * 0.5f;
            float halfSpread = (bulletsPerRow - 1) * 15f / 2f;

            float rad = (360f / totalCount) * index * Mathf.Deg2Rad;
            Vector3 center = caster.DrawPos;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * Radius, 0, Mathf.Sin(rad) * Radius);
            Vector3 orbPos = center + offset;

            for (int i = 0; i < bulletsPerRow; i++)
            {
                float angleOffset = -halfSpread + i * 15f;
                float finalAngle = baseAngle + angleOffset;

                Vector3 dir = new Vector3(
                    Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(finalAngle * Mathf.Deg2Rad)
                );

                IntVec3 targetCell = (orbPos + dir * 30f).ToIntVec3();

                ThingDef bulletDef = ThingDef.Named(
                    isPink ? "Butterfly_RowA" : "Butterfly_RowD");

                Projectile proj = (Projectile)GenSpawn.Spawn(
                    bulletDef,
                    orbPos.ToIntVec3(),
                    Map);

                proj.Launch(
                    caster,
                    orbPos,
                    new LocalTargetInfo(targetCell),
                    new LocalTargetInfo(targetCell),
                    ProjectileHitFlags.All,
                    false,
                    null);
            }
        }

        private void DoLaserDamage()
        {
            float length = BaseLaserLength + (stage - 1) * 5f;
            float angle = (360f / totalCount) * index + age * 0.5f;

            Vector3 dir = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            Vector3 start = caster.DrawPos;
            Map map = Map;

            for (float d = 0; d <= length; d += 0.5f)
            {
                IntVec3 cell = (start + dir * d).ToIntVec3();
                if (!cell.InBounds(map)) continue;

                List<Thing> list = cell.GetThingList(map);

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Thing t = list[i];

                    if (t is Pawn p && p.Faction != caster.Faction)
                    {
                        p.TakeDamage(new DamageInfo(
                            DefDatabase<DamageDef>.GetNamed("DeathButterflyFloatingMoon"),
                            DamageAmount,
                            1f,
                            -1f,
                            caster));
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            float angle = (360f / totalCount) * index + age * 0.5f;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 center = caster.DrawPos;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * Radius,
                0,
                Mathf.Sin(rad) * Radius
            );

            Vector3 orbPos = center + offset;
            orbPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float alpha;

            if (age < FadeTicks)
                alpha = (float)age / FadeTicks;
            else if (age > Lifetime - FadeTicks)
            {
                float fade = (float)(age - (Lifetime - FadeTicks)) / FadeTicks;
                alpha = 1f - fade;
            }
            else
                alpha = 1f;

            float sizeJitter = MaxSize + Rand.Range(-0.1f, 0.1f);

            string orbPath = isPink
                ? "Other/spellBulletDc000"
                : "Other/bulletDb001";

            Material orbMat = MaterialPool.MatFrom(
                orbPath,
                ShaderDatabase.MoteGlow,
                Color.white * alpha);

            Matrix4x4 orbMatrix = Matrix4x4.TRS(
                orbPos,
                Quaternion.identity,
                new Vector3(sizeJitter, 1f, sizeJitter)
            );

            Graphics.DrawMesh(MeshPool.plane10, orbMatrix, orbMat, 0);

            float length = BaseLaserLength + (stage - 1) * 5f;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)).normalized;
            Quaternion rot = Quaternion.LookRotation(dir) *
                             Quaternion.Euler(0, -90, 0);

            Vector3 laserPos = orbPos + dir * (length / 2f);
            laserPos.y = orbPos.y;

            string laserPath = isPink
                ? "Other/objectAe000"
                : "Other/bulletDa001";

            Material laserMat = MaterialPool.MatFrom(
                laserPath,
                ShaderDatabase.MoteGlow,
                Color.white * alpha);

            Matrix4x4 laserMatrix = Matrix4x4.TRS(
                laserPos,
                rot,
                new Vector3(length, 1f, LaserWidth)
            );

            Graphics.DrawMesh(MeshPool.plane10, laserMatrix, laserMat, 0);
        }
    }

    public class Stance_ResurrectionButterfly : Stance_Busy
    {
        public Stance_ResurrectionButterfly() { }
        public Stance_ResurrectionButterfly(int ticks, LocalTargetInfo focus, Verb verb) : base(ticks, focus, verb) { }

        public override void StanceTick()
        {
            this.ticksLeft--;
            if (this.ticksLeft <= 0)
            {
                this.Expire();
            }
        }
        public override void StanceDraw() { }
    }
}