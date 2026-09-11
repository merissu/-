using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace merissu
{
    public class HediffCompProperties_ToxicAura : HediffCompProperties
    {
        public float radius = 2f;
        public int applyIntervalTicks = 60;
        public HediffDef toxicHediff;
        public float toxicSeverity = 0.02f;
        public bool causeBerserk;
        public SongDef songDef;
        public FleckDef gasFleck;
        public int fxIntervalTicks = 10;

        public HediffCompProperties_ToxicAura()
        {
            compClass = typeof(HediffComp_ToxicAura);
        }
    }

    public class HediffComp_ToxicAura : HediffComp
    {
        private int tickCounter;
        private int fxCounter;

        private HashSet<Pawn> berserkTriggered = new HashSet<Pawn>();
        private List<Pawn> berserkTriggeredList;

        private bool musicPlayed = false;

        public HediffCompProperties_ToxicAura Props =>
            (HediffCompProperties_ToxicAura)props;

        public override void CompPostMake()
        {
            base.CompPostMake();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null) return;

            if (Props.songDef != null && !musicPlayed)
            {
                Find.MusicManagerPlay.ForcePlaySong(Props.songDef, false);
                musicPlayed = true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            fxCounter++;
            if (fxCounter >= Props.fxIntervalTicks)
            {
                fxCounter = 0;
                SpawnToxicFogPuffs();
            }

            tickCounter++;
            if (tickCounter < Props.applyIntervalTicks) return;
            tickCounter = 0;

            Pawn owner = Pawn;
            if (owner == null || owner.Map == null) return;

            IntVec3 center = owner.Position;

            List<Pawn> pawnsSnapshot =
                owner.Map.mapPawns.AllPawnsSpawned.ToList();

            foreach (Pawn pawn in pawnsSnapshot)
            {
                if (pawn == null) continue;
                if (pawn == owner) continue;
                if (pawn.Dead || pawn.Downed) continue;
                if (!pawn.HostileTo(owner)) continue;

                IntVec3 pos = pawn.Position;
                if (System.Math.Abs(pos.x - center.x) > Props.radius) continue;
                if (System.Math.Abs(pos.z - center.z) > Props.radius) continue;

                if (Props.toxicHediff != null)
                {
                    Hediff poison = pawn.health.GetOrAddHediff(Props.toxicHediff);
                    poison.Severity += Props.toxicSeverity;
                }

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

        private void SpawnToxicFogPuffs()
        {
            Pawn owner = Pawn;
            if (owner == null || !owner.Spawned) return;
            if (Props.gasFleck == null) return;

            Map map = owner.Map;
            IntVec3 center = owner.Position;
            int r = Mathf.Max(1, Mathf.RoundToInt(Props.radius));

            int puffs = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < puffs; i++)
            {
                IntVec3 cell = center + new IntVec3(
                    Rand.RangeInclusive(-r, r + 1),
                    0,
                    Rand.RangeInclusive(-r, r + 1));

                if (!cell.InBounds(map)) continue;
                if (!cell.ShouldSpawnMotesAt(map)) continue;

                FleckCreationData data = FleckMaker.GetDataStatic(
                    cell.ToVector3Shifted()
                        + new Vector3(Rand.Range(-0.6f, 0.6f), 0f, Rand.Range(-0.6f, 0.6f)),
                    map,
                    Props.gasFleck,
                    Rand.Range(1.4f, 2.2f));

                data.rotation = Rand.Range(0f, 360f);
                data.rotationRate = Rand.Range(-10f, 10f);
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = Rand.Range(0.1f, 0.35f);
                data.solidTimeOverride = Rand.Range(1.2f, 2.2f);

                float v = Rand.Range(0.8f, 1f);
                data.instanceColor = new Color(v, v, v, Rand.Range(0.5f, 1f));

                map.flecks.CreateFleck(data);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            berserkTriggered.Clear();

            if (musicPlayed)
            {
                Find.MusicManagerPlay.ForcePlaySong(null, false);
                musicPlayed = false;
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            Scribe_Values.Look(ref fxCounter, "fxCounter", 0);
            Scribe_Values.Look(ref musicPlayed, "musicPlayed", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                berserkTriggeredList = berserkTriggered.ToList();
            }
            Scribe_Collections.Look(ref berserkTriggeredList, "berserkTriggered", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                berserkTriggered = berserkTriggeredList != null
                    ? new HashSet<Pawn>(berserkTriggeredList)
                    : new HashSet<Pawn>();
                berserkTriggeredList = null;
            }
        }
    }
}