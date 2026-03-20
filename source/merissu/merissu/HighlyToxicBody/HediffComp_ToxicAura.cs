using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class HediffComp_ToxicAura : HediffComp
    {
        private int tickCounter;

        // 已触发狂暴的敌人
        private HashSet<Pawn> berserkTriggered = new HashSet<Pawn>();

        // ★ 是否已经播放过音乐
        private bool musicPlayed = false;

        public HediffCompProperties_ToxicAura Props =>
            (HediffCompProperties_ToxicAura)props;

        public override void CompPostMake()
        {
            base.CompPostMake();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null) return;

            // ★ 进入 Hediff 时立刻播放音乐（只播一次）
            if (Props.songDef != null && !musicPlayed)
            {
                Find.MusicManagerPlay.ForcePlaySong(Props.songDef, false);
                musicPlayed = true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            tickCounter++;
            if (tickCounter < Props.applyIntervalTicks) return;
            tickCounter = 0;

            // 生成毒雾 Fleck 特效
            if (Pawn != null && Pawn.Map != null)
            {
                FleckMaker.Static(
                    Pawn.Position,
                    Pawn.Map,
                    DefDatabase<FleckDef>.GetNamed("Merissu_ToxicFog"),
                    1.2f
                );
            }

            Pawn owner = Pawn;
            if (owner == null || owner.Map == null) return;

            IntVec3 center = owner.Position;

            // 对 Pawn 列表做快照
            List<Pawn> pawnsSnapshot =
                owner.Map.mapPawns.AllPawnsSpawned.ToList();

            foreach (Pawn pawn in pawnsSnapshot)
            {
                if (pawn == null) continue;
                if (pawn == owner) continue;
                if (pawn.Dead || pawn.Downed) continue;
                if (!pawn.HostileTo(owner)) continue;

                // 方形范围
                IntVec3 pos = pawn.Position;
                if (System.Math.Abs(pos.x - center.x) > Props.radius) continue;
                if (System.Math.Abs(pos.z - center.z) > Props.radius) continue;

                // 持续施加毒素
                if (Props.toxicHediff != null)
                {
                    Hediff poison = pawn.health.GetOrAddHediff(Props.toxicHediff);
                    poison.Severity += Props.toxicSeverity;
                }

                // 狂暴（只一次）
                if (Props.causeBerserk
                    && !berserkTriggered.Contains(pawn)
                    && pawn.mindState?.mentalStateHandler != null)
                {
                    berserkTriggered.Add(pawn);
                    pawn.mindState.mentalStateHandler
                        .TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            berserkTriggered.Clear();

            // ★ Hediff 移除 → 停止强制音乐
            if (musicPlayed)
            {
                Find.MusicManagerPlay.ForcePlaySong(null, false);
                musicPlayed = false;
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref musicPlayed, "musicPlayed", false);
        }
    }
}
