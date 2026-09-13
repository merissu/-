using RimWorld;
using Verse;

namespace merissu
{
    public class HediffCompProperties_TenshiFlight : HediffCompProperties
    {
        public HediffDef flyingHediffDef;

        // 浮空视觉高度（格）。原版 0.6；SkyLimit 的 heightFactor 默认 2
        public float flightHeightFactor = 3f;

        // 影子不透明度。原版 0.5，越大越厚；0.85~0.95 就很实了
        public float flightShadowOpacity = 0.85f;

        // 影子大小。原版 0.75
        public float flightShadowScale = 0.75f;

        // 叠画次数：有效不透明度 = 1-(1-opacity)^passes
        public int flightShadowPasses = 1;

        public HediffCompProperties_TenshiFlight()
        {
            compClass = typeof(HediffComp_TenshiFlight);
        }
    }
    public class HediffComp_TenshiFlight : HediffComp
    {
        public HediffCompProperties_TenshiFlight Props =>
            (HediffCompProperties_TenshiFlight)props;

        private Pawn P => parent.pawn;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            EnsureFlight();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = P;
            if (pawn == null || !pawn.Spawned || pawn.Dead)
                return;

            // 兜底：加 hediff 之前就已经在走路的，直接掐掉
            if (pawn.pather != null && pawn.pather.Moving)
                pawn.pather.StopDead();

            EnsureFlight();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            StopFlight();
        }

        private void EnsureFlight()
        {
            Pawn pawn = P;
            if (pawn == null || pawn.health == null)
                return;

            // 倒地时原版会 ForceLand，别硬顶；起来后 tick 会自动重新起飞
            if (pawn.Downed)
                return;

            HediffDef flyDef = Props.flyingHediffDef;
            if (flyDef != null &&
                pawn.health.hediffSet.GetFirstHediffOfDef(flyDef) == null)
            {
                pawn.health.AddHediff(flyDef);
            }

            Pawn_FlightTracker tracker = FlightCompatUtility.EnsureFlightTracker(pawn);
            if (tracker == null || tracker.Flying)
                return;

            // MaxFlightTime 是 cacheable 属性，不清缓存可能读到 5 秒前的旧值
            StatDefOf.MaxFlightTime.Worker.ClearCacheForThing(pawn);

            tracker.StartFlying();
        }

        private void StopFlight()
        {
            Pawn pawn = P;
            if (pawn == null || pawn.health == null)
                return;

            // 灵力飞行还开着的话，别把共用的飞行标记和飞行状态拆了
            if (FlightCompatUtility.IsAnyFlightEnabled(pawn))
                return;

            HediffDef flyDef = Props.flyingHediffDef;
            if (flyDef != null)
            {
                Hediff flyHediff = pawn.health.hediffSet.GetFirstHediffOfDef(flyDef);
                if (flyHediff != null)
                    pawn.health.RemoveHediff(flyHediff);
            }

            Pawn_FlightTracker tracker = FlightCompatUtility.EnsureFlightTracker(pawn);
            if (tracker != null && tracker.Flying)
                tracker.ForceLand();   // public，不用反射
        }
    }
}
