using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class Ability_DeadButterfly : Ability
    {
        private const int TotalDuration = 180;

        private const float PowerCost = 1.0f;
        public Ability_DeadButterfly() : base() { }
        public Ability_DeadButterfly(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < PowerCost)
                {
                    return "灵力不足"; 
                }

                return base.CanCast;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (pawn == null || pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null)
            {
                hp.Severity -= PowerCost;
            }
            pawn.jobs.StopAll();

            Job wait = JobMaker.MakeJob(RimWorld.JobDefOf.Wait_Combat, TotalDuration);
            wait.expiryInterval = TotalDuration;
            wait.checkOverrideOnExpire = false;
            pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);

            pawn.stances.SetStance(new Stance_DeadButterfly(TotalDuration));

            AddInvincible();

            SpawnController();

            return true;
        }

        private void AddInvincible()
        {
            if (pawn.health.hediffSet.GetFirstHediffOfDef(
                HediffDef.Named("DeadButterflyInvincible")) == null)
            {
                Hediff hediff = HediffMaker.MakeHediff(
                    HediffDef.Named("DeadButterflyInvincible"),
                    pawn);
                pawn.health.AddHediff(hediff);
            }
        }

        private void RemoveInvincible()
        {
            Hediff h = pawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDef.Named("DeadButterflyInvincible"));

            if (h != null)
                pawn.health.RemoveHediff(h);
        }

        public class Stance_DeadButterfly : Stance_Busy
        {
            private Pawn pawnRef;

            public Stance_DeadButterfly() { }

            public Stance_DeadButterfly(int ticks)
                : base(ticks, LocalTargetInfo.Invalid, null)
            {
            }

            public override void StanceTick()
            {
                base.StanceTick();

                if (ticksLeft <= 0)
                {
                    Expire();
                }
            }

            protected override void Expire()
            {
                if (stanceTracker?.pawn != null)
                {
                    Pawn p = stanceTracker.pawn;

                    Hediff h = p.health.hediffSet
                        .GetFirstHediffOfDef(HediffDef.Named("DeadButterflyInvincible"));

                    if (h != null)
                        p.health.RemoveHediff(h);
                }

                base.Expire();
            }

            public override void StanceDraw()
            {
            }
        }

        private void SpawnController()
        {
            Thing t = ThingMaker.MakeThing(
                ThingDef.Named("DeadButterflyController"));

            if (t is Thing_DeadButterflyController ctrl)
            {
                ctrl.caster = pawn;
                GenSpawn.Spawn(ctrl, pawn.Position, pawn.Map);
            }
        }
    }


    public class Thing_DeadButterflyController : Thing
    {
        public Pawn caster;
        private int age;

        private const int RingInterval = 15;
        private const int RingsPerCycle = 4;   
        private const int TotalCycles = 3;
        private static readonly SoundDef ShotSound =
    DefDatabase<SoundDef>.GetNamed("yuyukoshot");

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || !caster.Spawned)
            {
                Destroy();
                return;
            }

            Position = caster.Position;

            int totalRings = RingsPerCycle * TotalCycles;
            int totalDuration = RingInterval * totalRings;

            if (age % RingInterval == 1)
            {
                int ringIndex = (age - 1) / RingInterval;

                if (ringIndex < totalRings)
                {
                    if (ringIndex % 2 == 0)
                        SpawnRing("DeadButterflyA");
                    else
                        SpawnRing("DeadButterflyB");
                }
            }

            if (age > totalDuration + 60)
                Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
        }

        private void SpawnRing(string defName)
        {
            ShotSound?.PlayOneShot(new TargetInfo(Position, Map));

            int count = 50;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Thing t = ThingMaker.MakeThing(ThingDef.Named(defName));
                if (t is Thing_DeadButterflyBullet bullet)
                {
                    bullet.direction = dir.normalized;
                    bullet.caster = caster;
                    GenSpawn.Spawn(bullet, caster.Position, caster.Map);
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    public class Thing_DeadButterflyBullet : Thing
    {
        public Vector3 direction;
        public Pawn caster;

        private int age;

        private const float Speed = 0.15f;
        private const float MaxDistance = 25f;   
        private const int FadeDuration = 30;     
        private const float DamageAmount = 12f;
        private static readonly DamageDef DamageDefMoon =
            DefDatabase<DamageDef>.GetNamed("DeathButterflyFloatingMoon");

        private Vector3 realPosition;
        private Vector3 startPos;

        private bool fading;
        private int fadeTick;

        private static Mesh mesh;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            realPosition = DrawPos;
            startPos = realPosition;

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.vertices = new Vector3[]
                {
                new Vector3(-0.5f,0,-0.5f),
                new Vector3(0.5f,0,-0.5f),
                new Vector3(-0.5f,0,0.5f),
                new Vector3(0.5f,0,0.5f)
                };
                mesh.uv = new Vector2[]
                {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1)
                };
                mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
                mesh.RecalculateNormals();
            }
        }
        private void CheckHit()
        {
            if (Map == null) return;

            List<Thing> thingList = Position.GetThingList(Map);

            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Pawn pawn)
                {
                    if (pawn == caster) continue;
                    if (pawn.Dead) continue;

                    if (caster != null && pawn.Faction == caster.Faction)
                        continue;

                    DamageInfo dinfo = new DamageInfo(
                        DamageDefMoon,
                        DamageAmount,
                        0f,
                        -1f,
                        caster,
                        null,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        this);

                    pawn.TakeDamage(dinfo);
                }
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            realPosition += direction * Speed;
            Position = realPosition.ToIntVec3();

            CheckHit();

            float dist = Vector3.Distance(startPos, realPosition);

            if (!fading && dist >= MaxDistance)
            {
                fading = true;
                fadeTick = 0;
            }

            if (fading)
            {
                fadeTick++;

                if (fadeTick >= FadeDuration)
                    Destroy();
            }
        }

        private const int WingSwitchInterval = 6;
        private static MaterialPropertyBlock propertyBlock;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            bool wide = (age / WingSwitchInterval) % 2 == 0;
            float widthScale = wide ? 1f : 0.5f;

            Vector3 scale = new Vector3(widthScale, 1f, 1f);
            Quaternion rot = Quaternion.LookRotation(direction);

            Material mat = MaterialPool.MatFrom(
                def.graphicData.texPath,
                ShaderDatabase.MoteGlow);

            float alpha = 1f;
            if (fading)
                alpha = 1f - (float)fadeTick / FadeDuration;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Matrix4x4 matrix = Matrix4x4.TRS(
                realPosition + Vector3.up * 0.2f,
                rot,
                scale
            );

            Graphics.DrawMesh(mesh, matrix, mat, 0, null, 0, propertyBlock);
        }
    }
}
