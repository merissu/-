using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    public class CompProperties_YoumuRapidSlash : CompProperties_AbilityEffect
    {
        public float maxChainRange = 30f;
        public float maxBehindDistance = 15f;
        public float maxCasterDistance = 30f;
        public int chainIntervalTicks = 12;

        public CompProperties_YoumuRapidSlash()
        {
            compClass = typeof(CompAbilityEffect_YoumuRapidSlash);
        }
    }

    public class CompAbilityEffect_YoumuRapidSlash : CompAbilityEffect
    {
        private new CompProperties_YoumuRapidSlash Props =>
            (CompProperties_YoumuRapidSlash)props;

        private Pawn chainingCaster;
        private Pawn currentTarget;
        private HashSet<Pawn> hitTargets;
        private int cooldownTicks;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null || !target.IsValid)
                return;

            if (target.Pawn == null)
            {
                ExecuteSlash(caster, caster.Position, target.Cell);
                return;
            }

            chainingCaster = caster;
            currentTarget = target.Pawn;
            hitTargets = new HashSet<Pawn>();
            cooldownTicks = 0;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (chainingCaster == null || currentTarget == null)
                return;

            if (cooldownTicks-- > 0)
                return;

            if (currentTarget.Dead || !currentTarget.Spawned)
            {
                TrySwitchTargetOrEnd();
                cooldownTicks = Props.chainIntervalTicks;
                return;
            }

            hitTargets.Add(currentTarget);

            IntVec3 start = chainingCaster.Position;
            IntVec3 end = FindBehindCell(chainingCaster, currentTarget);

            ExecuteSlash(chainingCaster, start, end);

            TrySwitchTargetOrEnd();
            cooldownTicks = Props.chainIntervalTicks;
        }

        private void TrySwitchTargetOrEnd()
        {
            Pawn next = FindNextTarget(chainingCaster, chainingCaster.Map, hitTargets);

            if (next != null)
            {
                currentTarget = next;
            }
            else if (currentTarget.Dead || !currentTarget.Spawned)
            {
                EndChain();
            }
        }

        private void EndChain()
        {
            Pawn pawn = chainingCaster;

            chainingCaster = null;
            currentTarget = null;
            hitTargets = null;

            if (pawn != null && pawn.Spawned)
            {
                pawn.jobs?.StopAll();

                Job stand = JobMaker.MakeJob(RimWorld.JobDefOf.Wait, pawn.Position);
                stand.expiryInterval = 1; 
                stand.checkOverrideOnExpire = true;

                pawn.jobs.StartJob(
                    stand,
                    JobCondition.InterruptForced,
                    null,
                    false,
                    true
                );
            }
        }

        private void ExecuteSlash(Pawn caster, IntVec3 start, IntVec3 end)
        {
            Map map = caster.Map;

            caster.Position = end;

            SoundDef.Named("sakuraflash")
                .PlayOneShot(new TargetInfo(end, map));

            SpawnFlash(start, end, map);
            DoPathDamage(start, end, map, caster);
        }
        private IntVec3 FindBehindCell(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            Vector3 dir = (target.Position - caster.Position).ToVector3().normalized;
            IntVec3 best = target.Position;

            for (int i = 1; i <= Props.maxBehindDistance; i++)
            {
                IntVec3 cell = (target.Position.ToVector3() + dir * i).ToIntVec3();
                if (!cell.InBounds(map) || !cell.Standable(map)) continue;
                if (caster.Position.DistanceTo(cell) > Props.maxCasterDistance) continue;
                best = cell;
            }

            return best;
        }
        private Pawn FindNextTarget(Pawn caster, Map map, HashSet<Pawn> hit)
        {
            return map.mapPawns.AllPawnsSpawned
                .Where(p =>
                    p != caster &&
                    !p.Dead &&
                    !hit.Contains(p) &&
                    p.HostileTo(caster) &&
                    p.Position.DistanceTo(caster.Position) <= Props.maxChainRange)
                .OrderBy(p => p.Position.DistanceTo(caster.Position))
                .FirstOrDefault();
        }
        private void SpawnFlash(IntVec3 start, IntVec3 end, Map map)
        {
            Mote_YoumuFlash mote =
                ThingMaker.MakeThing(ThingDef.Named("Mote_YoumuFlash")) as Mote_YoumuFlash;

            mote.start = start.ToVector3Shifted();
            mote.end = end.ToVector3Shifted();
            mote.SetDelay(3);

            GenSpawn.Spawn(mote, start, map);
        }
        private void DoPathDamage(IntVec3 start, IntVec3 end, Map map, Pawn caster)
        {
            HashSet<Pawn> hit = new HashSet<Pawn>();

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start, end))
            {
                foreach (IntVec3 near in GenRadial.RadialCellsAround(cell, 1.2f, true))
                {
                    if (!near.InBounds(map)) continue;

                    foreach (Thing t in near.GetThingList(map).ToList())
                    {
                        Pawn p = t as Pawn;
                        if (p == null || p == caster || hit.Contains(p)) continue;

                        hit.Add(p);

                        p.TakeDamage(new DamageInfo(
                            DamageDefOf.Cut,
                            1000f,
                            0f,
                            -1f,
                            caster
                        ));
                    }
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    public class Mote_YoumuFlash : Thing
    {
        public Vector3 start;
        public Vector3 end;

        private int age;
        private int delayTicks;
        private const int LifeTime = 12;

        private Material mat;
        private static readonly MaterialPropertyBlock pb = new MaterialPropertyBlock();

        public void SetDelay(int ticks) => delayTicks = ticks;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mat = MaterialPool.MatFrom("Other/YoumuFlash", ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            if (delayTicks-- > 0) return;
            if (++age >= LifeTime) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (delayTicks > 0) return;

            Vector3 dir = end - start;
            float length = dir.magnitude;
            if (length < 0.01f) return;

            Vector3 center = (start + end) * 0.5f;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Quaternion rot =
                Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f);

            float width = 0.8f * Mathf.Max(1f, length);
            float height = 0.2f;

            float alpha = 1f - (float)age / LifeTime;
            pb.SetColor("_Color", new Color(1f, 1f, 1f, alpha));

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(center, rot, new Vector3(width, 1f, height)),
                mat,
                0,
                null,
                0,
                pb
            );
        }
    }
}
