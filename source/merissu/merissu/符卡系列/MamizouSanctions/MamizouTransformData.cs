using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace merissu
{
    public class MamizouTransformData : IExposable
    {
        public Pawn originalPawn;
        public Pawn animalPawn;
        public int ticksRemaining;

        public void ExposeData()
        {
            Scribe_References.Look(ref originalPawn, "originalPawn");
            Scribe_References.Look(ref animalPawn, "animalPawn");
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 1800);
        }
    }

    public class MamizouTransformManager : GameComponent, IThingHolder
    {
        private List<MamizouTransformData> activeTransformations = new List<MamizouTransformData>();
        private ThingOwner<Pawn> hiddenPawns;

        public IThingHolder ParentHolder => null;

        public MamizouTransformManager(Game game)
        {
            hiddenPawns = new ThingOwner<Pawn>(this);
        }

        public bool IsTransformed(Pawn pawn)
        {
            for (int i = 0; i < activeTransformations.Count; i++)
            {
                if (activeTransformations[i].animalPawn == pawn || activeTransformations[i].originalPawn == pawn)
                    return true;
            }
            return false;
        }

        public void StartTransformation(Pawn target)
        {
            if (target == null || target.Dead) return;

            if (IsTransformed(target)) return;

            Map map = target.Map;
            IntVec3 pos = target.Position;

            PawnKindDef animalDef = null;
            if (MamizouAPI.CustomRaceToAnimal.ContainsKey(target.def))
            {
                animalDef = MamizouAPI.CustomRaceToAnimal[target.def];
            }
            else
            {
                PawnKindDef fallbackAnimal = DefDatabase<PawnKindDef>.GetNamedSilentFail("Pig") ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Rat");
                animalDef = DefDatabase<PawnKindDef>.AllDefsListForReading.Where(k =>
                    k.RaceProps.Animal && k.RaceProps.baseBodySize < 1.5f && k.RaceProps.baseBodySize > 0.3f
                ).RandomElementWithFallback(fallbackAnimal);
            }

            Pawn animal = PawnGenerator.GeneratePawn(animalDef, target.Faction);
            GenSpawn.Spawn(animal, pos, map);

            SpawnHitMotes(target.DrawPos, map);

            animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, true);

            target.DeSpawn(DestroyMode.Vanish);
            hiddenPawns.TryAdd(target, false);

            activeTransformations.Add(new MamizouTransformData
            {
                originalPawn = target,
                animalPawn = animal,
                ticksRemaining = 1800
            });
        }

        public override void GameComponentTick()
        {
            for (int i = activeTransformations.Count - 1; i >= 0; i--)
            {
                var data = activeTransformations[i];
                data.ticksRemaining--;

                if (data.animalPawn.Dead)
                {
                    HandleAnimalDeath(data);
                    activeTransformations.RemoveAt(i);
                    continue;
                }

                if (!data.animalPawn.Spawned && data.ticksRemaining > 0)
                {
                    data.originalPawn.Destroy(DestroyMode.Vanish);
                    activeTransformations.RemoveAt(i);
                    continue;
                }

                if (data.ticksRemaining <= 0)
                {
                    RestorePawn(data);
                    activeTransformations.RemoveAt(i);
                }
            }
        }

        private void RestorePawn(MamizouTransformData data)
        {
            Map map = data.animalPawn.Map;
            IntVec3 pos = data.animalPawn.Position;

            SpawnHitMotes(data.animalPawn.DrawPos, map);

            TransferDamage(data.animalPawn, data.originalPawn);

            data.animalPawn.Destroy(DestroyMode.Vanish);

            hiddenPawns.Remove(data.originalPawn);
            GenSpawn.Spawn(data.originalPawn, pos, map);
        }

        private void HandleAnimalDeath(MamizouTransformData data)
        {
            Corpse animalCorpse = data.animalPawn.Corpse;
            Map map = animalCorpse?.Map;
            IntVec3 pos = animalCorpse?.Position ?? data.animalPawn.Position;

            if (map != null)
            {
                SpawnHitMotes(pos.ToVector3Shifted(), map);
                animalCorpse?.Destroy(DestroyMode.Vanish);

                hiddenPawns.Remove(data.originalPawn);
                GenSpawn.Spawn(data.originalPawn, pos, map);

                data.originalPawn.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 9999f));
            }
        }

        private void TransferDamage(Pawn from, Pawn to)
        {
            float hpPercentLost = 1f - from.health.summaryHealth.SummaryHealthPercent;
            if (hpPercentLost > 0)
            {
                float damageAmount = to.health.summaryHealth.SummaryHealthPercent * 100f * hpPercentLost;
                to.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount));
            }

            Thing attachedFire = from.GetAttachment(ThingDefOf.Fire);
            if (attachedFire != null)
            {
                Fire fire = attachedFire as Fire;
                if (fire != null)
                {
                    to.TryAttachFire(fire.fireSize, from);
                }
            }
        }

        private void SpawnHitMotes(Vector3 center, Map map)
        {
            string[] motes = { "Mote_MamizouHitSmokeA", "Mote_MamizouHitSmokeB", "Mote_MamizouHitSmokeC", "Mote_MamizouHitSmokeD" };
            for (int i = 0; i < 40; i++)
            {
                Mote_MamizouHitSmoke mote = (Mote_MamizouHitSmoke)ThingMaker.MakeThing(ThingDef.Named(motes.RandomElement()));
                GenSpawn.Spawn(mote, center.ToIntVec3(), map);
                mote.Init(center);
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren) { }
        public ThingOwner GetDirectlyHeldThings() => hiddenPawns;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref activeTransformations, "activeTransformations", LookMode.Deep);
            Scribe_Deep.Look(ref hiddenPawns, "hiddenPawns", this);

            if (activeTransformations == null) activeTransformations = new List<MamizouTransformData>();
            if (hiddenPawns == null) hiddenPawns = new ThingOwner<Pawn>(this);
        }
    }
}