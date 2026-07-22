using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class Mote_UdongeForwardShrink : MoteThrown
    {
        private const float ShrinkDelay = 0.8f;
        private const float ShrinkDuration = 0.25f;
        private bool startedShrinking = false;

        private int lastDamageTick = -9999;
        private const int DamageIntervalTicks = 6;
        private const float DamageRadius = 0.5f;

        public Thing launcher;
        public DamageDef damageDef;
        public int damageAmount = 5;
        public float armorPenetration = 999f;
        public ThingDef weaponDef;

        protected override void TimeInterval(float deltaTime)
        {
            if (!base.Destroyed && (Flying || Skidding))
            {
                Vector3 v = NextExactPosition(deltaTime);
                IntVec3 intVec = new IntVec3(v);
                if (intVec != base.Position)
                {
                    if (!intVec.InBounds(base.Map))
                    {
                        Destroy();
                        return;
                    }
                    if (def.mote.collide && intVec.Filled(base.Map))
                    {
                        WallHit();
                        return;
                    }
                }
                base.Position = intVec;
                exactPosition = v;

                if (def.mote.rotateTowardsMoveDirection && velocity != default(Vector3))
                    exactRotation = velocity.AngleFlat();
                else
                    exactRotation += rotationRate * deltaTime;

                velocity += def.mote.acceleration * deltaTime;
                if (def.mote.speedPerTime != 0f)
                    Speed = Mathf.Max(Speed + def.mote.speedPerTime * deltaTime, 0f);

                if (airTimeLeft > 0f)
                {
                    airTimeLeft -= deltaTime;
                    if (airTimeLeft < 0f) airTimeLeft = 0f;
                }
                if (Skidding)
                {
                    Speed *= skidSpeedMultiplierPerTick;
                    rotationRate *= skidSpeedMultiplierPerTick;
                    if (Speed < 0.02f) Speed = 0f;
                }
            }

            if (base.Destroyed) return;

            if (base.Spawned && Map != null)
            {
                int curTick = Find.TickManager.TicksGame;
                if (curTick - lastDamageTick >= DamageIntervalTicks)
                {
                    lastDamageTick = curTick;
                    DoContinuousDamage();
                }
            }

            if (AgeSecs >= ShrinkDelay)
            {
                if (!startedShrinking)
                    startedShrinking = true;

                float elapsed = AgeSecs - ShrinkDelay;
                float progress = Mathf.Clamp01(elapsed / ShrinkDuration);
                float newScale = Mathf.Lerp(2.4f, 0.01f, progress);
                linearScale = new Vector3(newScale, linearScale.y, newScale);

                if (progress >= 1f)
                    Destroy();
            }
        }

        private void DoContinuousDamage()
        {
            IntVec3 centerCell = new IntVec3(exactPosition);

            foreach (IntVec3 c in GenAdj.CellsOccupiedBy(centerCell, Rot4.North, new IntVec2(3, 3)))
            {
                if (!c.InBounds(Map)) continue;
                CheckCellForDamage(c);
            }
        }

        private void CheckCellForDamage(IntVec3 cell)
        {
            List<Thing> thingList = cell.GetThingList(Map);
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing t = thingList[i];
                if (t is Pawn pawn && !pawn.Destroyed && pawn != launcher)
                {
                    DamageInfo dinfo = new DamageInfo(
                        damageDef ?? DamageDefOf.Bomb,
                        damageAmount,
                        armorPenetration,
                        -1f,
                        launcher,
                        null,
                        weaponDef
                    );
                    pawn.TakeDamage(dinfo);
                }
            }
        }
    }
    public class AttackMode_WaveGunChargeExplode : WaveGunAttackMode
    {
        public override string ModeName => "WaveGunChargeExplode";
        protected override string ProjectileDefName => "MindStarExplosive";
        protected override string SoundDefName => "udongeshotA";

        public override int BurstCount => 1;
        public override int TicksBetweenShots => 1;
        public override float WarmupTime => 1f;
        public override bool PlaySoundOnEveryShot => true;

        public override LocalTargetInfo? GetAdjustedTarget(Verb_WaveGunRandomShoot verb, LocalTargetInfo target, int shotIndex)
        {
            Vector3 casterPos = verb.caster.DrawPos;
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            Vector3 dir = targetPos - casterPos;
            dir.y = 0;

            float dist = dir.magnitude;
            if (dist < 5f && dist > 0.001f)
            {
                Vector3 newPos = casterPos + dir.normalized * 5f;
                return new LocalTargetInfo(newPos.ToIntVec3());
            }
            return null;
        }

        public override bool OverrideCastShot(Verb_WaveGunRandomShoot verb, LocalTargetInfo target)
        {
            Map map = verb.caster.Map;
            if (map == null) return false;

            ThingDef projectileDef = verb.Projectile;
            if (projectileDef == null) return false;

            var adjusted = GetAdjustedTarget(verb, target, 0);
            if (adjusted.HasValue)
                target = adjusted.Value;

            ShootLine resultingLine;
            if (!verb.TryFindShootLineFromTo(verb.caster.Position, target, out resultingLine))
                return false;

            Vector3 casterPos = verb.caster.DrawPos;
            Thing launcher = verb.caster;
            Thing equipment = verb.EquipmentSource;

            Vector3 targetPos = target.Cell.ToVector3Shifted();
            Vector3 toTarget = targetPos - casterPos;
            toTarget.y = 0f;
            float baseAngle = toTarget.AngleFlat();
            float targetDist = Mathf.Min(toTarget.magnitude, 20f); 

            const int bulletCount = 5;
            const float totalSpread = 90f;

            for (int i = 0; i < bulletCount; i++)
            {
                float offset = -totalSpread / 2f + (totalSpread / (bulletCount - 1)) * i;
                float angle = baseAngle + offset;

                Vector3 dirVec = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad));
                IntVec3 targetCell = (casterPos + dirVec * targetDist).ToIntVec3();

                Projectile proj = (Projectile)GenSpawn.Spawn(projectileDef, resultingLine.Source, map);
                LocalTargetInfo projTarget = new LocalTargetInfo(targetCell);
                ProjectileHitFlags hitFlags = ProjectileHitFlags.All;

                proj.Launch(launcher, casterPos, targetCell, projTarget, hitFlags, false, equipment);
            }

            return true;
        }
    }

    public class Projectile_UdongeExplosive : Projectile
    {
        private int ticksAlive = 0;

        protected override void Tick()
        {
            base.Tick();
            ticksAlive++;
            if (this.Map != null && ticksAlive % 2 == 0)
            {
                SpawnTrailMote();
            }
        }

        private void SpawnTrailMote()
        {
            ThingDef moteDef = ThingDef.Named("Mote_UdongeVisionTrail");
            if (moteDef == null) return;

            MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
            mote.exactPosition = this.ExactPosition;

            float flightAngle = (this.destination - this.origin).AngleFlat();
            mote.exactRotation = flightAngle;
            mote.Scale = Rand.Range(0.4f, 0.7f);
            mote.rotationRate = 0f;

            GenSpawn.Spawn(mote, this.Position, this.Map);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            IntVec3 pos = this.Position;
            Vector3 exactPos = this.ExactPosition;
            float flightAngle = (this.destination - this.origin).AngleFlat();

            base.Impact(hitThing, blockedByShield);

            if (map == null) return;

            SoundDef.Named("udongeshotB")?.PlayOneShot(new TargetInfo(pos, map));
            Do3x3Damage(pos, map);
            SpawnCenterSpinMotes(exactPos, map);
            SpawnForwardShrinkMote(exactPos, flightAngle, map);
        }

        private void Do3x3Damage(IntVec3 center, Map map)
        {
            DamageDef dmgDef = this.def.projectile.damageDef ?? DamageDefOf.Bomb;
            int dmgAmount = this.def.projectile.damageAmountBase;
            float armorPen = this.def.projectile.armorPenetrationBase;

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    IntVec3 targetCell = center + new IntVec3(x, 0, z);
                    if (!targetCell.InBounds(map)) continue;

                    List<Thing> thingList = targetCell.GetThingList(map);
                    for (int i = thingList.Count - 1; i >= 0; i--)
                    {
                        Thing t = thingList[i];
                        if (t is Pawn || t is Building || t.def.useHitPoints)
                        {
                            DamageInfo dinfo = new DamageInfo(dmgDef, dmgAmount, armorPen, -1f, this.launcher, null, this.def);
                            t.TakeDamage(dinfo);
                        }
                    }
                }
            }
        }

        private void SpawnCenterSpinMotes(Vector3 exactPos, Map map)
        {
            ThingDef moteDef = ThingDef.Named("Mote_UdongeCenterSpin");
            if (moteDef == null) return;

            for (int i = 0; i < 3; i++)
            {
                MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(moteDef);
                mote.exactPosition = exactPos;
                mote.exactRotation = Rand.Range(0f, 360f);
                mote.Scale = 1.0f + i * 0.8f;
                mote.rotationRate = Rand.Range(-360f, 360f);
                GenSpawn.Spawn(mote, exactPos.ToIntVec3(), map);
            }
        }

        private void SpawnForwardShrinkMote(Vector3 exactPos, float angle, Map map)
        {
            ThingDef moteDef = ThingDef.Named("Mote_UdongeForwardShrink");
            if (moteDef == null) return;

            var mote = (Mote_UdongeForwardShrink)ThingMaker.MakeThing(moteDef);
            mote.exactPosition = exactPos;
            mote.exactRotation = angle;
            mote.Scale = 2.4f;
            mote.SetVelocity(angle, 8f);

            mote.launcher = this.launcher;
            mote.damageDef = this.def.projectile.damageDef;
            mote.damageAmount = this.def.projectile.damageAmountBase;
            mote.armorPenetration = this.def.projectile.armorPenetrationBase;
            mote.weaponDef = this.def;

            GenSpawn.Spawn(mote, exactPos.ToIntVec3(), map);
        }
    }
}