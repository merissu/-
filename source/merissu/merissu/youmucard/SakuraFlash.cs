using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class CompProperties_SakuraFlash : CompProperties_AbilityEffect
    {
        public CompProperties_SakuraFlash()
        {
            compClass = typeof(CompAbilityEffect_SakuraFlash);
        }
    }

    public class CompAbilityEffect_SakuraFlash : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = parent.pawn;
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 1f)
            {
                if (throwMessages)
                {
                    Messages.Message("灵力不足 (需要1层)", MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
        public override bool GizmoDisabled(out string reason)
        {
            Pawn pawn = parent.pawn;
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 1f)
            {
                reason = "灵力不足";
                return true;
            }

            reason = null;
            return false; 
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            Map map = pawn.Map;

            if (!target.IsValid || map == null)
                return;

            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null && hp.Severity >= 1f)
            {
                hp.Severity -= 1f; 
            }
            else
            {
                return; 
            }

            SoundDef.Named("sakuraflash").PlayOneShot(new TargetInfo(pawn.Position, map));

            IntVec3 startPos = pawn.Position;
            IntVec3 endPos = target.Cell;

            base.Apply(target, dest);
            pawn.Position = target.Cell;

            SpawnPathEffects(startPos, endPos, map);
        }
        private void SpawnPathEffects(IntVec3 start, IntVec3 end, Map map)
        {
            int i = 0;
            int interval = 1;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start, end))
            {
                Mote_SakuraFlashStatic mote = (Mote_SakuraFlashStatic)ThingMaker.MakeThing(ThingDef.Named("Mote_SakuraFlash"));
                mote.delayTicks = i * interval;
                mote.caster = parent.pawn; 
                GenSpawn.Spawn(mote, cell, map);
                i++;
            }

            Mote_SakuraFlashStatic endMote = (Mote_SakuraFlashStatic)ThingMaker.MakeThing(ThingDef.Named("Mote_SakuraFlash"));
            endMote.delayTicks = i * interval;
            endMote.caster = parent.pawn;
            GenSpawn.Spawn(endMote, end, map);
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_SakuraFlashStatic : Thing
    {
        public int delayTicks = 0;
        public Pawn caster;
        private int age;
        private bool burstTriggered = false;

        private const int LifeTime = 35;
        private const int BurstDelay = 60; 

        private const float BaseWidth = 1.5f;
        private const float BaseHeight = 6.5f;
        private const float MaxSkewAngle = 1f;
        private const float PosJitterRange = 0.2f;

        private Material mat;
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        private float instanceSkew;
        private Vector3 instanceOffset;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom("Other/spellBulletDd000", ShaderDatabase.MoteGlow);
            instanceSkew = Rand.Range(-MaxSkewAngle, MaxSkewAngle);
            instanceOffset = new Vector3(Rand.Range(-PosJitterRange, PosJitterRange), 0, Rand.Range(-PosJitterRange, PosJitterRange));
        }

        protected override void Tick()
        {
            if (delayTicks > 0)
            {
                delayTicks--;
                return;
            }

            if (age == BurstDelay && !burstTriggered && Map != null)
            {
                TriggerBurst();
                burstTriggered = true;
            }

            age++;
            if (age >= Mathf.Max(LifeTime, BurstDelay + 5)) Destroy();
        }

        private void TriggerBurst()
        {
            Vector3 center = this.DrawPos;

            int burstCount = Rand.RangeInclusive(2, 4);
            for (int i = 0; i < burstCount; i++)
            {
                Vector3 offset = new Vector3(Rand.Range(-2f, 2f), 0, Rand.Range(-2f, 2f));
                if (offset.magnitude < 0.6f) offset = offset.normalized * 0.6f;

                Vector3 spawnPos = center + offset;
                Mote_DualLayerBurst b = (Mote_DualLayerBurst)ThingMaker.MakeThing(ThingDef.Named("Mote_DualLayerBurst"));

                b.caster = this.caster; 
                b.exactPosition = spawnPos;

                GenSpawn.Spawn(b, spawnPos.ToIntVec3(), Map);
            }

            for (int i = 0; i < 12; i++)
            {
                Mote_FallingPetal c = (Mote_FallingPetal)ThingMaker.MakeThing(ThingDef.Named("Mote_FallingPetal"));
                GenSpawn.Spawn(c, center.ToIntVec3(), Map);
                c.Initialize(center);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (delayTicks > 0 || age >= LifeTime) return;

            float t = (float)age / LifeTime;
            float alpha = 1f - t;
            propBlock.SetColor("_Color", new Color(1, 1, 1, alpha));

            float currentWidth = Mathf.Lerp(BaseWidth, 0f, t);
            Vector3 scale = new Vector3(currentWidth, 1f, BaseHeight);
            Vector3 pos = drawLoc + instanceOffset;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            pos.z += BaseHeight * 0.2f;

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, scale);
            matrix.m02 += instanceSkew * BaseWidth;
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, propBlock);
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_DualLayerBurst : Thing
    {
        private int age;
        private const int TotalLife = 25;
        public Vector3 exactPosition;
        public Pawn caster;
        private static readonly MaterialPropertyBlock pb = new MaterialPropertyBlock();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ApplyBurstDamage();
            }
        }

        private void ApplyBurstDamage()
        {
            IntVec3 centerCell = exactPosition.ToIntVec3();

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    IntVec3 targetCell = centerCell + new IntVec3(x, 0, z);

                    if (!targetCell.InBounds(Map)) continue;

                    List<Thing> targets = targetCell.GetThingList(Map);
                    for (int i = targets.Count - 1; i >= 0; i--)
                    {
                        Pawn victim = targets[i] as Pawn;
                        if (victim != null && victim != caster)
                        {
                            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, 1000f, 0f, -1f, caster);
                            victim.TakeDamage(dinfo);
                        }
                    }
                }
            }
        }
        protected override void Tick()
        {
            age++;
            if (age >= TotalLife) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float t = (float)age / TotalLife;
            float alpha = Mathf.Sin(t * Mathf.PI);

            pb.SetColor("_Color", new Color(1, 1, 1, alpha));
            Vector3 scale = new Vector3(2f, 1f, 2f);
            Vector3 pos = exactPosition;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.01f;

            Material m1 = MaterialPool.MatFrom("Other/spellBulletDc000", ShaderDatabase.MoteGlow);
            Material m2 = MaterialPool.MatFrom("Other/spellBulletDb000", ShaderDatabase.MoteGlow);

            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, scale), m1, 0, null, 0, pb);
            pos.y += 0.01f;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, scale), m2, 0, null, 0, pb);
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_FallingPetal : Thing
    {
        private int age;
        private Vector3 pos;
        private Vector3 vel;
        private float rot;
        private float rotVel;
        private float size;
        private static readonly MaterialPropertyBlock pb = new MaterialPropertyBlock();

        public void Initialize(Vector3 start)
        {
            pos = start;
            size = Rand.Range(1f, 2f);
            rot = Rand.Range(0, 360f);
            float angle = Rand.Range(0, 360f) * Mathf.Deg2Rad;
            float spd = Rand.Range(0.04f, 0.12f);
            vel = new Vector3(Mathf.Cos(angle) * spd, 0, Mathf.Sin(angle) * spd);
            vel.z += Rand.Range(0.05f, 0.1f);
            rotVel = Rand.Range(-8f, 8f);
        }

        protected override void Tick()
        {
            age++;
            pos += vel;
            vel.z -= 0.003f; 
            vel.x *= 0.96f; 
            rot += rotVel;
            rotVel *= 0.99f;
            if (age >= 100) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float alpha = 1f - (float)age / 100f;
            pb.SetColor("_Color", new Color(1, 1, 1, alpha));
            Vector3 renderPos = pos;
            renderPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.05f;
            Matrix4x4 matrix = Matrix4x4.TRS(renderPos, Quaternion.AngleAxis(rot, Vector3.up), new Vector3(size, 1, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Other/spellBulletDa000", ShaderDatabase.MoteGlow), 0, null, 0, pb);
        }
    }
}