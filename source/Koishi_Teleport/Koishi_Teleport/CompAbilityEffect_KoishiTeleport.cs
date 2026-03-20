using RimWorld;
using Verse;

namespace THSIR_KoishiTeleport
{
    public class CompAbilityEffect_KoishiTeleport : CompAbilityEffect
    {
        public new CompProperties_AbilityTeleport Props =>
            (CompProperties_AbilityTeleport)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            if (caster == null || !target.IsValid)
                return;

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.DestroyedOrNull())
                return;

            Map map = caster.Map;
            if (map == null)
                return;

            // ========= 视觉效果 =========
            parent.AddEffecterToMaintain(
                EffecterDefOf.Skip_EntryNoDelay.Spawn(caster, map),
                caster.Position,
                60
            );

            parent.AddEffecterToMaintain(
                EffecterDefOf.Skip_Exit.Spawn(targetPawn.Position, map),
                targetPawn.Position,
                60
            );

            // ========= 瞬移 =========
            caster.Position = targetPawn.Position;

            // ========= 如果目标不是自己，执行处决 =========
            if (targetPawn != caster)
            {
                ExecutePawn(targetPawn, caster);
            }

            // ========= Hediff 处理 =========
            Hediff liberation =
                caster.health.hediffSet.GetFirstHediffOfDef(
                    HediffDefOf.TheLiberationOfTheId
                );

            if (liberation != null)
            {
                liberation.Severity -= 1f;
            }
        }

        /// <summary>
        /// 以 1.6 推荐方式处决 Pawn
        /// </summary>
        private static void ExecutePawn(Pawn victim, Pawn instigator)
        {
            if (victim.Dead || victim.Destroyed)
                return;

            // 如果你只是想“必死”，1.6 推荐直接 Kill
            victim.Kill(null);

            // —— 如果你以后想改成“表现为斩首 / 切割”
            // 可以换成：
            //
            // DamageInfo dinfo = new DamageInfo(
            //     DamageDefOf.ExecutionCut,
            //     99999f,
            //     instigator: instigator
            // );
            // victim.TakeDamage(dinfo);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return parent.pawn.Faction != Faction.OfPlayer
                   && target.Pawn != null;
        }
    }
}
