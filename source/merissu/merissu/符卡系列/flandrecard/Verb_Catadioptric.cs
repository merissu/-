using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace merissu
{
    public class Verb_Catadioptric : Verb_MeleeAttackDamage
    {
        private static int alternator = 0;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (caster == null || !caster.Spawned) return false;

            Thing target = targ.Thing;
            if (target == null) return false;
            if (target == caster) return true;
            if (target is Pawn p && p.IsPsychologicallyInvisible() && caster.HostileTo(p)) return false;
            if (ApparelPreventsShooting()) return false;

            CellRect rect = target.OccupiedRect();
            float range = EffectiveRange;
            float distSq = rect.ClosestDistSquaredTo(root);

            if (distSq > range * range) return false;
            if (distSq <= 1f) return true;
            if (!verbProps.requireLineOfSight) return true;

            IntVec3 closest = rect.ClosestCellTo(root);
            IntVec3 probe = closest + new IntVec3(
                Sign(root.x - closest.x), 0, Sign(root.z - closest.z));

            return GenSight.LineOfSight(root, probe, caster.Map, skipFirstCell: true);
        }

        private static int Sign(int v)
        {
            if (v > 0) return 1;
            if (v < 0) return -1;
            return 0;
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (!(CasterPawn is Pawn casterPawn) || target.Thing == null)
            {
                base.OrderForceTarget(target);
                return;
            }

            Job job = JobMaker.MakeJob(MerissuDefOf.Merissu_GreatswordSweep, target);
            job.verbToUse = this;
            job.playerForced = true;

            if (target.Thing is Pawn p) job.killIncappedTarget = p.Downed;

            casterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        protected override bool TryCastShot()
        {
            if (caster != null && caster.Spawned && caster.Map != null)
            {
                SpawnSwingEffect();
            }

            return base.TryCastShot();
        }

        private void SpawnSwingEffect()
        {
            Vector3 origin = caster.Position.ToVector3();
            Vector3 targetPos = CurrentTarget.HasThing
                ? CurrentTarget.Thing.DrawPos
                : CurrentTarget.Cell.ToVector3();

            Vector3 dir = targetPos - origin;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
            {
                IntVec3 facing = caster.Rotation.FacingCell;
                dir = new Vector3(facing.x, 0f, facing.z);
            }

            dir = dir.normalized;
            float aimAngle = dir.AngleFlat();

            bool useB = (alternator++ & 1) == 1;

            for (int dist = 1; dist <= 5; dist++)
            {
                SpawnOneEffect(origin, dir, dist, useB);
            }

            Pawn casterPawn = CasterPawn;
            if (casterPawn == null) return;

            Thing ctrl = ThingMaker.MakeThing(ThingDef.Named("LaevatainSweepController"));
            if (ctrl is Thing_LaevatainSweepController c)
            {
                c.caster = casterPawn;
                c.aimAngle = aimAngle;
                c.reverseSweep = useB;
            }
            GenSpawn.Spawn(ctrl, caster.Position, caster.Map);
        }

        private void SpawnOneEffect(Vector3 origin, Vector3 dir, int dist, bool useB)
        {
            IntVec3 spawnCell = (origin + dir * dist).ToIntVec3();
            if (!spawnCell.InBounds(caster.Map)) return;
            if (!spawnCell.Walkable(caster.Map)) return;

            Thing eff = ThingMaker.MakeThing(ThingDef.Named("LaevatainSwingEffect"));
            if (eff is Thing_LaevatainSwingEffect s)
            {
                s.size = dist + 2f;
                s.angle = dir.AngleFlat();
                s.variantB = useB;
            }
            GenSpawn.Spawn(eff, spawnCell, caster.Map);
        }
    }

    public class JobDriver_GreatswordSweep : JobDriver
    {
        private const int RepathInterval = 30;
        private Verb SweepVerb => job.verbToUse;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.targetA.Thing is IAttackTarget t)
            {
                pawn.Map.attackTargetReservationManager.Reserve(pawn, job, t);
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            yield return FightToil();
        }

        private Toil FightToil()
        {
            Toil toil = ToilMaker.MakeToil("GreatswordSweepFight");

            toil.tickAction = delegate ()
            {
                Thing target = TargetThingA;
                Verb verb = SweepVerb;

                if (target == null || verb == null || !verb.IsStillUsableBy(pawn))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (target is Pawn tp && tp.Downed && !job.killIncappedTarget)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                if (verb.CanHitTarget(TargetA))
                {
                    if (pawn.pather.Moving) pawn.pather.StopDead();
                    pawn.meleeVerbs.TryMeleeAttack(target, verb);
                    return;
                }

                if (!pawn.IsHashIntervalTick(RepathInterval)) return;

                if (!pawn.CanReach(TargetA, PathEndMode.Touch, Danger.Deadly))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!pawn.pather.Moving || pawn.pather.Destination != TargetA)
                {
                    pawn.pather.StartPath(TargetA, PathEndMode.Touch);
                }
            };

            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.activeSkill = () => SkillDefOf.Melee;

            return toil;
        }
    }

    [StaticConstructorOnStartup]
    public class Thing_LaevatainSwingEffect : Thing
    {
        public const int TotalFrames = 5;
        public const int TicksPerFrame = 2;
        public const int LifeTicks = TotalFrames * TicksPerFrame;

        public float size = 3f;
        public float angle = 0f;
        public bool variantB = false;

        private int age;
        private static readonly Material[] MatsA = new Material[TotalFrames];
        private static readonly Material[] MatsB = new Material[TotalFrames];

        static Thing_LaevatainSwingEffect()
        {
            for (int i = 0; i < TotalFrames; i++)
            {
                MatsA[i] = MaterialPool.MatFrom($"Weapons/Laevatain/fire_rod{i:D3}", ShaderDatabase.MoteGlow);
                MatsB[i] = MaterialPool.MatFrom($"Weapons/Laevatain/fire_upperrod{i:D3}", ShaderDatabase.MoteGlow);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref size, "size", 3f);
            Scribe_Values.Look(ref angle, "angle", 0f);
            Scribe_Values.Look(ref variantB, "variantB", false);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= LifeTicks) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = age / TicksPerFrame;
            if (frame < 0 || frame >= TotalFrames) return;

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, angle - 60f, 0f),
                new Vector3(size, 1f, size)
            );

            Material mat = variantB ? MatsB[frame] : MatsA[frame];
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }

    [StaticConstructorOnStartup]
    public class Thing_LaevatainSweepController : Thing
    {
        public Pawn caster;
        public float aimAngle;
        public bool reverseSweep = false;

        private const int Duration = 15;
        private const float TotalArcDegrees = 75f;
        private const int MaxRadius = 6;
        private const int RowInterval = 1;
        private const float RadiusStep = 0.5f;

        private const int MiniTotalFrames = 7;
        private const int MiniTicksPerFrame = 2;
        private const int MiniLifeTicks = MiniTotalFrames * MiniTicksPerFrame;
        private const float MiniSize = 1.5f;

        private const float FireChance = 0.15f;
        private const float FireSize = 0.5f;
        private const float DamagePerHit = 15f;
        private const float BuildingDamageMultiplier = 4f;

        private static readonly Material[] MiniMats = new Material[MiniTotalFrames];

        static Thing_LaevatainSweepController()
        {
            for (int i = 0; i < MiniTotalFrames; i++)
                MiniMats[i] = MaterialPool.MatFrom(
                    $"Weapons/Laevatain/expload_mini_{i:D3}",
                    ShaderDatabase.MoteGlow
                );
        }

        private int age;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref aimAngle, "aimAngle", 0f);
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref reverseSweep, "reverseSweep", false);
        }

        protected override void Tick()
        {
            base.Tick();

            if (caster == null || !caster.Spawned || caster.Map == null || caster.Map != Map)
            {
                Destroy();
                return;
            }

            if (age >= Duration + MiniLifeTicks)
            {
                Destroy();
                return;
            }

            if (age < Duration)
            {
                ApplyDamageForRow(age);
            }

            age++;
        }

        private void ApplyDamageForRow(int b)
        {
            Map map = Map;
            if (map == null) return;

            IntVec3 casterCell = caster.Position;

            float t = (float)b / (Duration - 1);
            if (reverseSweep) t = 1f - t;

            float rowAngle = aimAngle - TotalArcDegrees / 2f + t * TotalArcDegrees;
            float rad = rowAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            Vector3 origin = caster.Position.ToVector3Shifted();

            for (float r = 1f; r <= MaxRadius; r += RadiusStep)
            {
                IntVec3 cell = (origin + dir * r).ToIntVec3();

                if (!cell.InBounds(map)) continue;
                if (cell == casterCell) continue;

                if (Rand.Value < FireChance)
                {
                    FireUtility.TryStartFireIn(cell, map, FireSize, caster);
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing th = things[i];
                    if (th == this || th == caster) continue;

                    if (th is Pawn || th is Building || th.def.useHitPoints)
                    {
                        float dmg = DamagePerHit;
                        if (th is Building) dmg *= BuildingDamageMultiplier;

                        th.TakeDamage(new DamageInfo(
                            def: DamageDefOf.Flame,
                            amount: dmg,
                            armorPenetration: 1.5f,
                            instigator: caster
                        ));
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null || !caster.Spawned) return;

            float alt = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 origin = caster.Position.ToVector3Shifted();
            origin.y = alt;

            int maxB = Mathf.Min(age, Duration - 1);

            for (int b = 0; b <= maxB; b += RowInterval)
            {
                DrawOneRow(b, origin, alt);
            }

            if ((Duration - 1) % RowInterval != 0 && maxB == Duration - 1)
            {
                DrawOneRow(Duration - 1, origin, alt);
            }
        }

        private void DrawOneRow(int b, Vector3 origin, float alt)
        {
            int miniAge = age - b;
            if (miniAge < 0 || miniAge >= MiniLifeTicks) return;

            int frame = miniAge / MiniTicksPerFrame;
            if (frame < 0 || frame >= MiniTotalFrames) return;

            float t = (float)b / (Duration - 1);
            if (reverseSweep) t = 1f - t;

            float rowAngle = aimAngle - TotalArcDegrees / 2f + t * TotalArcDegrees;
            float rad = rowAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            Quaternion rot = Quaternion.Euler(0f, rowAngle, 0f);
            Vector3 scale = new Vector3(MiniSize, 1f, MiniSize);

            for (float r = 1f; r <= MaxRadius; r += RadiusStep)
            {
                Vector3 p = origin + dir * r;
                p.y = alt;
                Graphics.DrawMesh(
                    MeshPool.plane10,
                    Matrix4x4.TRS(p, rot, scale),
                    MiniMats[frame],
                    0
                );
            }
        }
    }

    [StaticConstructorOnStartup]
    public class Thing_LaevatainMiniEffect : Thing
    {
        private const int TotalFrames = 7;
        private const int TicksPerFrame = 4;

        public float angle = 0f;
        private int age;
        private static readonly Material[] Mats = new Material[TotalFrames];

        static Thing_LaevatainMiniEffect()
        {
            for (int i = 0; i < TotalFrames; i++)
                Mats[i] = MaterialPool.MatFrom($"Weapons/Laevatain/expload_mini_{i:D3}", ShaderDatabase.MoteGlow);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref angle, "angle", 0f);
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= TotalFrames * TicksPerFrame) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int frame = age / TicksPerFrame;
            if (frame < 0 || frame >= TotalFrames) return;

            Vector3 pos = drawLoc;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(0f, angle, 0f),
                new Vector3(1.5f, 1f, 1.5f)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, Mats[frame], 0);
        }
    }

    [StaticConstructorOnStartup]
    public static class MerissuGreatswordHarmony
    {
        static MerissuGreatswordHarmony()
        {
            new Harmony("merissu.greatswordreach").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_Pawn_JobTracker_StartJob
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn ___pawn, Job newJob)
        {
            if (newJob == null || newJob.def != RimWorld.JobDefOf.AttackMelee) return;
            if (___pawn?.equipment?.Primary?.def?.defName != "Laevatain") return;

            Verb_Catadioptric verb = ___pawn.meleeVerbs
                .GetUpdatedAvailableVerbsList(false)
                .Select(ve => ve.verb)
                .OfType<Verb_Catadioptric>()
                .FirstOrDefault();

            if (verb == null) return;

            newJob.def = MerissuDefOf.Merissu_GreatswordSweep;
            newJob.verbToUse = verb;
        }
    }
}