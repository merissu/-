using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace U.N.Owen
{
    public class JobDriver_SpawnWine : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Thing thing = this.job.GetTarget(TargetIndex.A).Thing;
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.AddFailCondition(() => !job.GetTarget(TargetIndex.A).HasThing);
            Thing Wine = this.job.GetTarget(TargetIndex.B).Thing;
            int k;
            k = Wine.stackCount;
            Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B, 1, k, null, false);
            yield return reserveFuel;
            this.job.count = k;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, false).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false, true, false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None, true, null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);
            yield return Toils_General.Wait(0.55f.SecondsToTicks(), TargetIndex.None);
            Toil toil = Toils_General.Wait(1.5f.SecondsToTicks(), TargetIndex.None);
            yield return toil;
            yield return Toils_General.Do(delegate
            {
                Thing Candy = this.job.GetTarget(TargetIndex.B).Thing;
                int x;
                for (x = Candy.stackCount; x > 0; x--)
                {
                    GenSpawn.Spawn(ThingDefOf.JunmaiDaiGinjoShu, thing.Position, thing.Map);
                }
                Candy.Destroy();
            });
            yield return Toils_General.Wait(0.35f.SecondsToTicks(), TargetIndex.None);
            yield break;
        }
        private const TargetIndex ChargerInd = TargetIndex.A;
    }
    public class Gungnir_BreakHeart : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            hitThing = this.intendedTarget.Thing;
            if (hitThing != null)
            {
                Pawn pawn = hitThing as Pawn;
                if (pawn != null)
                {
                    foreach (BodyPartRecord bodyPartRecord in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Middle, BodyPartDepth.Inside, null, null))
                    {
                        if (bodyPartRecord.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource) && !bodyPartRecord.def.tags.Contains(BodyPartTagDefOf.Mirrored) && !bodyPartRecord.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationSource))
                        {
                            DamageInfo dinfo = new DamageInfo(base.DamageDef, (float)this.DamageAmount, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, bodyPartRecord, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, false, true, QualityCategory.Normal, true, false);
                            dinfo.SetWeaponQuality(this.equipmentQuality);
                            hitThing.TakeDamage(dinfo);
                        }
                    }
                }
                else
                {
                    hitThing.TakeDamage(new DamageInfo(base.DamageDef, (float)this.DamageAmount, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true, false));
                }
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
    public class CompProperties_AbilityLaunchProjectileAuto : CompProperties_AbilityLaunchProjectile
    {
        public bool requireDrafted = true;
        public bool stopWhenTargetInvalid = true;

        public CompProperties_AbilityLaunchProjectileAuto()
        {
            compClass = typeof(CompAbilityEffect_LaunchProjectileAuto);
        }
    }
    public class CompAbilityEffect_LaunchProjectileAuto : CompAbilityEffect_LaunchProjectile
    {
        private bool autoActive;
        private LocalTargetInfo lastTarget = LocalTargetInfo.Invalid;
        private LocalTargetInfo lastDest = LocalTargetInfo.Invalid;

        public new CompProperties_AbilityLaunchProjectileAuto Props
            => (CompProperties_AbilityLaunchProjectileAuto)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest); // 原有发射逻辑
            autoActive = true;
            lastTarget = target;
            lastDest = dest;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!autoActive || parent?.pawn == null) return;

            Pawn pawn = parent.pawn;
            if (!pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                autoActive = false;
                return;
            }

            if (Props.requireDrafted && !pawn.Drafted) return;
            if (pawn.pather?.MovingNow == true || pawn.CurJobDef == JobDefOf.Goto)
            {
                autoActive = false;
                return;
            }
            // 避免打断别的正式工作
            if (pawn.CurJob != null && pawn.CurJob.ability != parent && !pawn.CurJob.def.isIdle) return;

            // 正在 warmup/casting 或硬直中
            if (parent.Casting) return;
            if (pawn.stances?.FullBodyBusy ?? false) return;

            // Ability 本体冷却/充能条件
            if (!parent.CanCast) return;

            if (!lastTarget.IsValid)
            {
                autoActive = false;
                return;
            }

            // 目标校验（范围/LoS/effect comp Valid）
            if (!parent.CanApplyOn(lastTarget) || !parent.verb.ValidateTarget(lastTarget, showMessages: false))
            {
                if (Props.stopWhenTargetInvalid) autoActive = false;
                return;
            }

            // 重新排队一次施法（会再次 warmup）
            parent.QueueCastingJob(lastTarget, lastDest);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref autoActive, "autoActive", defaultValue: false);
            Scribe_TargetInfo.Look(ref lastTarget, "lastTarget");
            Scribe_TargetInfo.Look(ref lastDest, "lastDest");
        }
    }
    public class DeathActionWorker_SpawnP : DeathActionWorker
    {

        // Token: 0x0600C294 RID: 49812 RVA: 0x00385D24 File Offset: 0x00383F24
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            int x;
            for (x = 8; x > 0; x--)
            {
                GenSpawn.Spawn(ThingDefOf.PowerPoint, corpse.Position, corpse.Map);
            }
            corpse.Destroy();
        }
    }
}
