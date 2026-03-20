using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class MerissuStartup
    {
        static MerissuStartup()
        {
            new Harmony("merissu.youmuclone").PatchAll();
        }
    }

    public class Ability_YoumuClone : Ability
    {
        public Ability_YoumuClone() : base() { }

        public Ability_YoumuClone(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                {
                    return "灵力不足";
                }

                return AcceptanceReport.WasAccepted;
            }
        }
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Hediff hp = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("FullPower"));
            if (hp != null)
            {
                hp.Severity -= 1f;
            }
            SoundDef.Named("youmuClone").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            if (!base.Activate(target, dest)) return false;

            Pawn caster = pawn;
            Map map = caster.Map;

            PawnGenerationRequest request = new PawnGenerationRequest(
                caster.kindDef,
                caster.Faction,
                PawnGenerationContext.NonPlayer,
                -1,
                true, false, false, false,
                true,
                0f,
                false, true, true, false, false
            );

            Pawn clone = PawnGenerator.GeneratePawn(request);
            clone.health.hediffSet.Clear();

            YoumuCloneCopyUtility.CopyFromMaster(caster, clone);

            foreach (SkillRecord s in caster.skills.skills)
            {
                SkillRecord cs = clone.skills.GetSkill(s.def);
                cs.Level = s.Level;
                cs.passion = s.passion;
            }

            if (clone.needs?.food != null)
                clone.needs.food.CurLevel = clone.needs.food.MaxLevel;
            if (clone.needs?.rest != null)
                clone.needs.rest.CurLevel = clone.needs.rest.MaxLevel;

            clone.apparel.DestroyAll();
            foreach (Apparel a in caster.apparel.WornApparel)
                clone.apparel.Wear((Apparel)ThingMaker.MakeThing(a.def, a.Stuff));

            clone.equipment.DestroyAllEquipment();
            if (caster.equipment.Primary != null)
                clone.equipment.AddEquipment(
                    (ThingWithComps)ThingMaker.MakeThing(
                        caster.equipment.Primary.def,
                        caster.equipment.Primary.Stuff
                    )
                );

            GenSpawn.Spawn(clone, caster.Position, map);

            Hediff_YoumuShadow shadow =
                (Hediff_YoumuShadow)clone.health.AddHediff(
                    HediffDef.Named("youmuClone")
                );
            shadow.masterPawn = caster;
            Hediff soulHediff = clone.health.AddHediff(HediffDef.Named("youmusoul"));
            return true;
        }
    }

    public class Hediff_YoumuShadow : HediffWithComps
    {
        public Pawn masterPawn;
        private bool returningToMaster = false;

        public override void Tick()
        {
            base.Tick();
            Pawn clone = pawn;

            if (masterPawn == null || masterPawn.DestroyedOrNull())
            {
                clone.Destroy(DestroyMode.Vanish);
                return;
            }

            if (clone.drafter != null && masterPawn.drafter != null)
            {
                clone.drafter.Drafted = masterPawn.drafter.Drafted;
            }

            if (clone.playerSettings != null)
                clone.playerSettings.hostilityResponse = HostilityResponseMode.Ignore;

            if (clone.needs?.food != null)
                clone.needs.food.CurLevel = clone.needs.food.MaxLevel;
            if (clone.needs?.rest != null)
                clone.needs.rest.CurLevel = clone.needs.rest.MaxLevel;

            if (!masterPawn.Drafted)
            {
                float distToMaster = (clone.Position - masterPawn.Position).LengthHorizontal;

                if (returningToMaster)
                {
                    if (distToMaster <= 8f)
                    {
                        returningToMaster = false;
                    }
                    else
                    {
                        if (clone.CurJobDef != RimWorld.JobDefOf.Goto)
                        {
                            Job followBack = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, masterPawn.Position);
                            followBack.playerForced = true;
                            clone.jobs.StartJob(followBack, JobCondition.InterruptForced);
                        }
                        return;
                    }
                }

                if (clone.CurJobDef == RimWorld.JobDefOf.AttackMelee)
                {
                    if (distToMaster > 10f)
                    {
                        returningToMaster = true;

                        Job followBack = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, masterPawn.Position);
                        followBack.playerForced = true;
                        clone.jobs.StartJob(followBack, JobCondition.InterruptForced);
                    }
                    return;
                }

                Pawn nearbyEnemy = (Pawn)GenClosest.ClosestThingReachable(
                    clone.Position,
                    clone.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                    PathEndMode.OnCell,
                    TraverseParms.For(clone),
                    10f,
                    x =>
                        x is Pawn p
                        && p.HostileTo(clone)
                        && !p.Downed
                        && !p.Dead
                        && GenSight.LineOfSight(clone.Position, p.Position, clone.Map)
                );

                if (nearbyEnemy != null)
                {
                    Job attack = JobMaker.MakeJob(RimWorld.JobDefOf.AttackMelee, nearbyEnemy);
                    attack.playerForced = true;
                    clone.jobs.StartJob(attack, JobCondition.InterruptForced);
                    return;
                }

                if (clone.CurJobDef != RimWorld.JobDefOf.Goto)
                {
                    Job follow = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, masterPawn.Position);
                    follow.playerForced = true;
                    clone.jobs.StartJob(follow, JobCondition.InterruptForced);
                }

                return;
            }


            if (clone.CurJobDef == RimWorld.JobDefOf.AttackMelee)
            {
                Pawn targetPawn = clone.CurJob?.targetA.Pawn;
                if (targetPawn != null)
                {
                    if ((clone.Position - targetPawn.Position).LengthHorizontal < 15f)
                    {
                        return;
                    }
                }
            }

            Job masterJob = masterPawn.CurJob;
            if (masterJob == null)
                return;

            JobDef mDef = masterJob.def;

            if (mDef == RimWorld.JobDefOf.AttackMelee)
            {
                Pawn targetPawn = masterJob.targetA.Pawn;
                if (targetPawn != null)
                {
                    if (clone.CurJob?.targetA.Pawn != targetPawn)
                    {
                        Job copy = JobMaker.MakeJob(RimWorld.JobDefOf.AttackMelee, targetPawn);
                        copy.playerForced = true;
                        clone.jobs.StartJob(copy, JobCondition.InterruptForced);
                    }
                }
                return;
            }

            if (mDef == RimWorld.JobDefOf.AttackStatic)
            {
                Pawn targetPawn = masterJob.targetA.Pawn;
                if (targetPawn != null)
                {
                    if (clone.CurJob?.targetA.Pawn != targetPawn)
                    {
                        Job copy = JobMaker.MakeJob(RimWorld.JobDefOf.AttackMelee, targetPawn);
                        copy.playerForced = true;
                        clone.jobs.StartJob(copy, JobCondition.InterruptForced);
                    }
                }
                return;
            }

            if (mDef == RimWorld.JobDefOf.Goto
                || mDef == RimWorld.JobDefOf.Wait_Combat
                || (masterPawn.stances != null && masterPawn.stances.curStance is Stance_Busy))
            {
                Pawn closestEnemy = (Pawn)GenClosest.ClosestThingReachable(
                    clone.Position,
                    clone.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                    PathEndMode.OnCell,
                    TraverseParms.For(clone),
                    10f,
                    x =>
                        x is Pawn p
                        && p.HostileTo(clone)
                        && !p.Downed
                        && !p.Dead
                        && GenSight.LineOfSight(clone.Position, p.Position, clone.Map)
                );

                if (closestEnemy != null)
                {
                    if (clone.CurJob?.targetA.Thing != closestEnemy)
                    {
                        Job hunt = JobMaker.MakeJob(RimWorld.JobDefOf.AttackMelee, closestEnemy);
                        clone.jobs.StartJob(hunt, JobCondition.InterruptForced);
                    }
                }
                else if (mDef == RimWorld.JobDefOf.Goto)
                {
                    if (masterJob.targetA.IsValid && clone.CurJobDef != RimWorld.JobDefOf.Goto)
                    {
                        Job follow = JobMaker.MakeJob(RimWorld.JobDefOf.Goto, masterJob.targetA);
                        clone.jobs.StartJob(follow, JobCondition.InterruptForced);
                    }
                }
            }
        }
        public override void PostRemoved()
        {
            base.PostRemoved();
            if (pawn != null && !pawn.Destroyed)
                pawn.Destroy(DestroyMode.Vanish);
        }
    }
}

[HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class Patch_CloneSkipPushing
    {
        public static bool Prefix(Pawn_PathFollower __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn?.health?.hediffSet.HasHediff(
                    HediffDef.Named("youmuClone")) == true)
            {
                return true;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_NoGizmos
    {
        public static bool Prefix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.health?.hediffSet.HasHediff(
                    HediffDef.Named("youmuClone")) == true)
            {
                __result = new List<Gizmo>();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonistPlayerControlled), MethodType.Getter)]
    public static class Patch_NotControlled
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (__instance.health?.hediffSet.HasHediff(
                    HediffDef.Named("youmuClone")) == true)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.BodySize), MethodType.Getter)]
    public static class Patch_NoCollision
    {
        public static void Postfix(Pawn __instance, ref float __result)
        {
            if (__instance.health?.hediffSet.HasHediff(
                    HediffDef.Named("youmuClone")) == true)
            {
                __result = 0f;
            }
        }
    }

    public static class YoumuCloneCopyUtility
    {
        public static void CopyFromMaster(Pawn master, Pawn clone)
        {
            clone.ageTracker.AgeBiologicalTicks = master.ageTracker.AgeBiologicalTicks;
            clone.ageTracker.AgeChronologicalTicks = master.ageTracker.AgeChronologicalTicks;
            clone.ageTracker.BirthAbsTicks = master.ageTracker.BirthAbsTicks;

            clone.Name = master.Name;
            clone.gender = master.gender;
            clone.story.Childhood = master.story.Childhood;
            clone.story.Adulthood = master.story.Adulthood;
            if (master.style != null && clone.style != null)
            {
                clone.style.beardDef = master.style.beardDef;
            }
            clone.story.traits.allTraits.Clear();
            foreach (Trait t in master.story.traits.allTraits)
                clone.story.traits.GainTrait(new Trait(t.def, t.Degree));

            if (ModsConfig.BiotechActive && master.genes != null && clone.genes != null)
            {
                clone.genes.SetXenotype(master.genes.Xenotype);
                clone.genes.xenotypeName = master.genes.xenotypeName;
                clone.genes.iconDef = master.genes.iconDef;

                while (clone.genes.Endogenes.Any()) clone.genes.RemoveGene(clone.genes.Endogenes[0]);
                while (clone.genes.Xenogenes.Any()) clone.genes.RemoveGene(clone.genes.Xenogenes[0]);

                foreach (Gene g in master.genes.Endogenes)
                    clone.genes.AddGene(g.def, false);

                foreach (Gene g in master.genes.Xenogenes)
                    clone.genes.AddGene(g.def, true);
            }

            clone.story.bodyType = master.story.bodyType;
            clone.story.headType = master.story.headType;
            clone.story.hairDef = master.story.hairDef;
            clone.story.HairColor = master.story.HairColor;

            clone.story.skinColorOverride = master.story.skinColorOverride;

            clone.relations?.ClearAllRelations();
            var memHandler = clone.needs?.mood?.thoughts?.memories;
            if (memHandler != null)
            {
                foreach (var memory in memHandler.Memories.ToList())
                    memHandler.RemoveMemory(memory);
            }

            clone.Drawer.renderer.SetAllGraphicsDirty();
        }
    }
