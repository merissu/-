using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class HediffCompProperties_TrackMaidKnife : HediffCompProperties
    {
        public ThingDef orbDef;
        public float radius = 1.2f;

        public HediffCompProperties_TrackMaidKnife()
        {
            this.compClass = typeof(HediffComp_TrackMaidKnife);
        }
    }

    public class HediffComp_TrackMaidKnife : HediffComp
    {
        private Thing_TrackMaidKnife_Orb spawnedOrb;
        public HediffCompProperties_TrackMaidKnife Props => (HediffCompProperties_TrackMaidKnife)props;

        public override void CompPostPostAdd(DamageInfo? dinfo) => SpawnOrb();

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(60))
            {
                if (spawnedOrb == null || spawnedOrb.Destroyed) SpawnOrb();
            }
        }

        private void SpawnOrb()
        {
            if (Pawn.Map != null && Props.orbDef != null)
            {
                spawnedOrb = (Thing_TrackMaidKnife_Orb)ThingMaker.MakeThing(Props.orbDef);
                spawnedOrb.Init(Pawn);
                GenSpawn.Spawn(spawnedOrb, Pawn.Position, Pawn.Map);
            }
        }

        public override void CompPostPostRemoved()
        {
            if (spawnedOrb != null && !spawnedOrb.Destroyed) spawnedOrb.Destroy();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref spawnedOrb, "spawnedOrb");
        }
    }

    public class Thing_TrackMaidKnife_Orb : Thing
    {
        private Pawn owner;
        private float currentAngle;
        private int age = 0;
        private int lastShotTick = -999;

        private List<Pawn> cachedTargets = new List<Pawn>();

        private const int TicksPerFrame = 3;
        private const int MaxFrameIndex = 7;
        new private const float DrawSize = 0.5f;

        private const float MaxAttackRange = 30f;
        private const int ShootIntervalTicks = 60;
        private const float FollowDist = 1.2f;
        private const float CombatDist = 1.8f;

        private static readonly HediffDef SpiritualPowerDef = HediffDef.Named("spiritualpower");
        private static readonly ThingDef ProjectileDef = ThingDef.Named("trackknife");
        private static readonly SoundDef ShootSound = SoundDef.Named("knife");

        private const float MinPowerToFire = 0.02f;
        private const float PowerCostPerShot = 0.01f;

        public void Init(Pawn owner) => this.owner = owner;

        private float GetCurrentSpiritualPower()
        {
            if (owner?.health?.hediffSet == null) return 0f;
            return owner.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef)?.Severity ?? 0f;
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (owner == null) return base.DrawPos;
                float rad = currentAngle * Mathf.Deg2Rad;
                float dist = owner.Drafted ? CombatDist : FollowDist;
                float xOffset = Mathf.Cos(rad) * dist;
                float zOffset = Mathf.Sin(rad) * dist;
                float floatingY = Mathf.Sin(age * 0.05f) * 0.1f;

                Vector3 pos = owner.DrawPos + new Vector3(xOffset, 0.5f, zOffset + floatingY);
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                return pos;
            }
        }

        protected override void Tick()
        {
            if (owner == null || owner.Dead || !owner.Spawned)
            {
                if (!Destroyed) this.Destroy();
                return;
            }

            if (this.Position != owner.Position) this.Position = owner.Position;
            age++;

            if (age % 30 == 0)
            {
                if (owner.Drafted && GetCurrentSpiritualPower() >= MinPowerToFire)
                {
                    UpdateCachedTargets();
                }
                else
                {
                    cachedTargets.Clear();
                }
            }

            if (cachedTargets.Count > 0)
            {
                Vector3 aimDir = (cachedTargets[0].DrawPos - owner.DrawPos).normalized;
                float targetAngle = Mathf.Atan2(aimDir.z, aimDir.x) * Mathf.Rad2Deg;
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 0.15f);

                if (Find.TickManager.TicksGame >= lastShotTick + ShootIntervalTicks)
                {
                    ShootAtTargets(cachedTargets);
                    lastShotTick = Find.TickManager.TicksGame;
                }
            }
            else
            {
                currentAngle = (currentAngle + 1.2f) % 360f;
            }
        }

        private void UpdateCachedTargets()
        {
            cachedTargets.Clear();
            IEnumerable<Thing> nearby = GenRadial.RadialDistinctThingsAround(owner.Position, owner.Map, MaxAttackRange, true);

            int found = 0;
            foreach (Thing t in nearby)
            {
                if (t is Pawn p && p.HostileTo(owner) && !p.Downed && !p.Dead && !p.IsPrisoner)
                {
                    if (GenSight.LineOfSight(owner.Position, p.Position, owner.Map))
                    {
                        cachedTargets.Add(p);
                        found++;
                    }
                }
                if (found >= 3) break; 
            }
        }

        private void ShootAtTargets(List<Pawn> targets)
        {
            if (ProjectileDef == null || targets.Count == 0) return;

            if (ShootSound != null)
                ShootSound.PlayOneShot(new TargetInfo(owner.Position, owner.Map));

            Pawn currentTarget = targets[0];
            Projectile projectile = (Projectile)GenSpawn.Spawn(ProjectileDef, DrawPos.ToIntVec3(), owner.Map);

            if (projectile != null)
            {
                projectile.Launch(owner, DrawPos, currentTarget, currentTarget, ProjectileHitFlags.All, false, null);
                ConsumeSpiritualPower(PowerCostPerShot);
            }
        }

        private void ConsumeSpiritualPower(float amount)
        {
            Hediff hediff = owner.health.hediffSet.GetFirstHediffOfDef(SpiritualPowerDef);
            if (hediff != null) hediff.Severity -= amount;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float totalAnimTime = MaxFrameIndex * TicksPerFrame;
            float pingPongValue = Mathf.PingPong(age, totalAnimTime);
            int frame = Mathf.RoundToInt(pingPongValue / TicksPerFrame);

            Material mat = MaterialPool.MatFrom($"Projectiles/MaidKnife/Orb{frame:D3}", ShaderDatabase.MoteGlow);

            Quaternion rotation = Quaternion.AngleAxis(-currentAngle, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, rotation, new Vector3(DrawSize, 1f, DrawSize));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref currentAngle, "currentAngle");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref lastShotTick, "lastShotTick");
        }
    }
}