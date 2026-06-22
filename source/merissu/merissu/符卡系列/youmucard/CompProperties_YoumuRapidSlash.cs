using RimWorld;
using System.Collections.Generic;
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
        public int chainIntervalTicks = 1;

        public CompProperties_YoumuRapidSlash()
        {
            compClass = typeof(CompAbilityEffect_YoumuRapidSlash);
        }
    }

    public class CompAbilityEffect_YoumuRapidSlash : CompAbilityEffect
    {
        private new CompProperties_YoumuRapidSlash Props => (CompProperties_YoumuRapidSlash)props;

        private Pawn chainingCaster;
        private Pawn currentTarget;
        private Pawn lastHitTarget;
        private int cooldownTicks;
        private int currentSlashCount;

        private bool bouncingAway;
        private IntVec3 bounceDest = IntVec3.Invalid;

        private bool isSuperDashing;
        private IntVec3 dashDest = IntVec3.Invalid;

        private static readonly SoundDef SlashSound = DefDatabase<SoundDef>.GetNamedSilentFail("sakuraflash");
        private static readonly ThingDef FlashMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_YoumuFlash");

        private readonly HashSet<Pawn> tmpHitPawns = new HashSet<Pawn>();
        private readonly HashSet<IntVec3> tmpVisitedCells = new HashSet<IntVec3>();

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent?.pawn;
            if (caster == null || caster.Map == null || !target.IsValid)
                return;

            if (target.Pawn == null)
            {
                ExecuteSlash(caster, caster.Position, target.Cell);
                return;
            }

            chainingCaster = caster;
            currentTarget = target.Pawn;
            lastHitTarget = null;
            cooldownTicks = 0;
            currentSlashCount = 0;

            bouncingAway = false;
            isSuperDashing = false;

            YoumuRapidSlashVisualState.SetActive(caster, true);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (chainingCaster == null) return;
            if (cooldownTicks-- > 0) return;

            if (chainingCaster.Dead || !chainingCaster.Spawned || chainingCaster.Downed || chainingCaster.Map == null)
            {
                EndChain();
                return;
            }

            if (isSuperDashing)
            {
                ExecuteSlash(chainingCaster, chainingCaster.Position, dashDest);
                isSuperDashing = false;
                cooldownTicks = Props.chainIntervalTicks;
                return;
            }

            if (bouncingAway)
            {
                ExecuteSlash(chainingCaster, chainingCaster.Position, bounceDest);
                bouncingAway = false;
                lastHitTarget = null;
                cooldownTicks = Props.chainIntervalTicks;
                return;
            }

            if (currentTarget == null || currentTarget.Dead || !currentTarget.Spawned || currentTarget.Map != chainingCaster.Map)
            {
                DetermineNextAction();
                if (chainingCaster == null) return;
                if (isSuperDashing || bouncingAway)
                {
                    cooldownTicks = Props.chainIntervalTicks;
                    return;
                }
            }

            if (currentTarget != null && !currentTarget.Dead && currentTarget.Spawned && currentTarget.Map == chainingCaster.Map)
            {
                IntVec3 start = chainingCaster.Position;
                IntVec3 end = FindBehindCell(chainingCaster, currentTarget);

                ExecuteSlash(chainingCaster, start, end);

                currentSlashCount++;
                lastHitTarget = currentTarget;

                DetermineNextAction();
                cooldownTicks = Props.chainIntervalTicks;
            }
            else
            {
                EndChain();
            }
        }

        private void DetermineNextAction()
        {
            if (chainingCaster == null || chainingCaster.Map == null)
            {
                EndChain();
                return;
            }

            Pawn next = FindNextTarget(chainingCaster, Props.maxChainRange);

            if (next == null && currentSlashCount > 0 && currentSlashCount % 50 == 0)
            {
                Pawn nextFar = FindNextTarget(chainingCaster, Props.maxChainRange * 2f);
                if (nextFar != null)
                {
                    currentTarget = nextFar;
                    isSuperDashing = true;
                    dashDest = CalculateDashDest(chainingCaster, nextFar);
                    return;
                }
            }

            if (next != null)
            {
                currentTarget = next;
                return;
            }

            if (lastHitTarget != null && !lastHitTarget.Dead && !lastHitTarget.Downed && lastHitTarget.Spawned && lastHitTarget.Map == chainingCaster.Map)
            {
                currentTarget = lastHitTarget;
                bouncingAway = true;
                bounceDest = CalculateBounceDest(chainingCaster, lastHitTarget);
                return;
            }

            EndChain();
        }

        private IntVec3 CalculateBounceDest(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            if (map == null) return caster.Position;

            Vector3 dirFromTarget = (caster.Position - target.Position).ToVector3();
            if (dirFromTarget.sqrMagnitude < 0.01f) dirFromTarget = Vector3.right;
            dirFromTarget.y = 0f;
            dirFromTarget.Normalize();

            Quaternion rot = Quaternion.Euler(0f, 45f, 0f);
            Vector3 bounceDir = rot * dirFromTarget;

            float halfRange = Props.maxChainRange * 0.5f;

            for (int i = Mathf.FloorToInt(halfRange); i >= 1; i--)
            {
                IntVec3 cell = (target.Position.ToVector3() + bounceDir * i).ToIntVec3();

                if (cell.InBounds(map) && cell.Standable(map) &&
                    GenSight.LineOfSight(caster.Position, cell, map) &&
                    GenSight.LineOfSight(cell, target.Position, map))
                {
                    return cell;
                }
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, halfRange, true))
            {
                if (cell.DistanceTo(target.Position) >= halfRange - 1f &&
                    cell.InBounds(map) && cell.Standable(map) &&
                    GenSight.LineOfSight(caster.Position, cell, map) &&
                    GenSight.LineOfSight(cell, target.Position, map))
                {
                    return cell;
                }
            }

            return caster.Position;
        }

        private IntVec3 CalculateDashDest(Pawn caster, Pawn farTarget)
        {
            Map map = caster.Map;
            if (map == null) return farTarget.Position;

            Vector3 dir = (caster.Position - farTarget.Position).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
            dir.Normalize();

            float targetDist = Props.maxChainRange * 0.8f;

            for (int i = Mathf.FloorToInt(targetDist); i >= 2; i--)
            {
                IntVec3 cell = (farTarget.Position.ToVector3() + dir * i).ToIntVec3();
                if (cell.InBounds(map) && cell.Standable(map) && GenSight.LineOfSight(cell, farTarget.Position, map))
                    return cell;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(farTarget.Position, Props.maxChainRange, true))
            {
                if (cell.InBounds(map) && cell.Standable(map) && GenSight.LineOfSight(cell, farTarget.Position, map))
                    return cell;
            }

            return farTarget.Position;
        }

        private Pawn FindNextTarget(Pawn caster, float range)
        {
            Map map = caster.Map;
            if (map == null) return null;

            float bestDistSq = range * range;
            Pawn best = null;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p == lastHitTarget) continue;
                if (p.Dead || p.Downed || !p.Spawned) continue;
                if (!p.HostileTo(caster)) continue;

                float distSq = (p.Position - caster.Position).LengthHorizontalSquared;
                if (distSq <= bestDistSq)
                {
                    bestDistSq = distSq;
                    best = p;
                }
            }

            return best;
        }

        private void EndChain()
        {
            Pawn pawn = chainingCaster;

            chainingCaster = null;
            currentTarget = null;
            lastHitTarget = null;
            bouncingAway = false;
            isSuperDashing = false;

            YoumuRapidSlashVisualState.SetActive(pawn, false);

            if (pawn != null && pawn.Spawned && pawn.jobs != null)
            {
                pawn.jobs.StopAll();

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
            if (map == null) return;

            if (!end.InBounds(map) || !end.Standable(map))
                end = start;

            caster.Position = end;

            SlashSound?.PlayOneShot(new TargetInfo(end, map));

            SpawnFlash(start, end, map);
            DoPathDamage(start, end, map, caster);
        }

        private IntVec3 FindBehindCell(Pawn caster, Pawn target)
        {
            Map map = caster.Map;
            if (map == null) return target.Position;

            Vector3 dir = (target.Position - caster.Position).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return target.Position;
            dir.Normalize();

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

        private void SpawnFlash(IntVec3 start, IntVec3 end, Map map)
        {
            if (FlashMoteDef == null) return;

            Mote_YoumuFlash mote = ThingMaker.MakeThing(FlashMoteDef) as Mote_YoumuFlash;
            if (mote == null) return;

            mote.start = start.ToVector3Shifted();
            mote.end = end.ToVector3Shifted();
            mote.SetDelay(3);

            GenSpawn.Spawn(mote, start, map);
        }

        private void DoPathDamage(IntVec3 start, IntVec3 end, Map map, Pawn caster)
        {
            tmpHitPawns.Clear();
            tmpVisitedCells.Clear();

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start, end))
            {
                foreach (IntVec3 near in GenRadial.RadialCellsAround(cell, 1.2f, true))
                {
                    if (!near.InBounds(map)) continue;
                    if (!tmpVisitedCells.Add(near)) continue; 

                    List<Thing> things = near.GetThingList(map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Pawn p = things[i] as Pawn;
                        if (p == null || p == caster || tmpHitPawns.Contains(p)) continue;

                        tmpHitPawns.Add(p);

                        p.TakeDamage(new DamageInfo(
                            DamageDefOf.Cut,
                            100f,
                            999f,
                            -1f,
                            caster
                        ));
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                YoumuRapidSlashVisualState.SetActive(parent?.pawn, false);
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

            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 0f);

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