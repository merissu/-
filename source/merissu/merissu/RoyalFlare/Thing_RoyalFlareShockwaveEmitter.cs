using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.Sound;

namespace merissu
{
    public class Thing_RoyalFlareShockwaveEmitter : Thing
    {
        private Pawn caster;
        private int age;
        private Thing rangeIndicator;

        private Thing_RoyalFlareConeLight[] coneLights;
        private Thing_RoyalFlareConeLightReverse[] reverseConeLights;
        private bool conesSpawned;
        private bool reverseConesSpawned;

        public static readonly Vector3 SunOffset = new Vector3(0f, 0f, 1.6f);
        public Vector3 SunDrawPos => caster.DrawPos + SunOffset;

        private const int Duration = 300;
        private const int DamageInterval = 2;
        private const int VisualInterval = 10;
        private const int ScorchInterval = 5; 

        private const float DamageRadius = 25f;
        private const float DamageAmount = 30f;
        private const int ShockwaveParticles = 8;

        private const string LockHediffName = "RoyalFlareConeLight";

        public void Init(Pawn pawn)
        {
            caster = pawn;
            if (caster == null || caster.Map == null) return;

            if (!caster.health.hediffSet.HasHediff(HediffDef.Named(LockHediffName)))
            {
                caster.health.AddHediff(HediffDef.Named(LockHediffName));
            }

            rangeIndicator = ThingMaker.MakeThing(ThingDef.Named("RoyalFlareRangeIndicator"));

            if (rangeIndicator != null)
            {
                GenSpawn.Spawn(rangeIndicator, SunDrawPos.ToIntVec3(), caster.Map);
                (rangeIndicator as Thing_RoyalFlareRangeIndicator)?.Init(caster);
            }

            SpawnConeLights();
            SpawnReverseConeLights();
        }

        protected override void Tick()
        {
            base.Tick();
            age++;

            if (caster == null || caster.Destroyed || caster.Map == null)
            {
                Cleanup();
                return;
            }

            Position = SunDrawPos.ToIntVec3();

            if (age == 1)
            {
                SoundDef shockwaveSound = SoundDef.Named("RoyalFlareShockwave");
                if (shockwaveSound != null)
                {
                    shockwaveSound.PlayOneShot(new TargetInfo(Position, caster.Map));
                }
            }

            if (age % VisualInterval == 0)
                SpawnShockwave();

            if (age % DamageInterval == 0)
                PerformDamageAndPlantCleanup();

            if (age % ScorchInterval == 0)
                PerformVisualEffects();

            if (age >= Duration)
                Cleanup();
        }

        private void PerformDamageAndPlantCleanup()
        {
            if (caster?.Map == null) return;
            Map map = caster.Map;
            Vector3 center = SunDrawPos;
            float radiusSq = DamageRadius * DamageRadius;

            IEnumerable<Thing> targets = GenRadial.RadialDistinctThingsAround(center.ToIntVec3(), map, DamageRadius, true);
            foreach (Thing t in targets.ToList())
            {
                if (t == null || !t.Spawned || t == caster || t == this) continue;
                if ((t.Position.ToVector3() - center).sqrMagnitude > radiusSq) continue;

                if (t is Plant)
                {
                    t.Destroy(DestroyMode.Vanish);
                    continue;
                }

                bool shouldDamage = false;
                if (t is Pawn p)
                {
                    if (p.HostileTo(caster) && !p.Dead) shouldDamage = true;
                }
                else if (t is Building || (t.def.category == ThingCategory.Item) || t is Corpse)
                {
                    if (t.Faction == null || t.Faction.HostileTo(caster.Faction)) shouldDamage = true;
                }

                if (shouldDamage)
                {
                    t.TakeDamage(new DamageInfo(DamageDefOf.Flame, DamageAmount, 0, -1, caster));
                }
            }
        }

        private void PerformVisualEffects()
        {
            Map map = caster.Map;
            IntVec3 centerCell = SunDrawPos.ToIntVec3();
            ThingDef scorchDef = DefDatabase<ThingDef>.GetNamed("Filth_ScorchMark", false);
            ThingDef ashDef = DefDatabase<ThingDef>.GetNamed("Filth_Ash", false);

            int numCells = GenRadial.NumCellsInRadius(DamageRadius);
            for (int i = 0; i < 20; i++)
            {
                IntVec3 c = centerCell + GenRadial.RadialPattern[Rand.Range(0, numCells)];
                if (!c.InBounds(map)) continue;

                if (Rand.Value < 0.5f)
                {
                    ThingDef filthToSpawn = scorchDef ?? ashDef;
                    if (filthToSpawn != null) FilthMaker.TryMakeFilth(c, map, filthToSpawn, 1);
                }

                if (Rand.Value < 0.1f)
                {
                    FleckMaker.ThrowSmoke(c.ToVector3Shifted(), map, Rand.Range(1.0f, 2.0f));
                }

                if (Rand.Value < 0.05f)
                {
                    FleckMaker.ThrowMicroSparks(c.ToVector3Shifted(), map);
                }
            }
        }

        private void SpawnShockwave()
        {
            if (caster.Map == null) return;
            Vector3 center = SunDrawPos;
            Thing_RoyalFlareShockwave wave = (Thing_RoyalFlareShockwave)ThingMaker.MakeThing(ThingDef.Named("RoyalFlareShockwave"));
            wave.caster = caster;
            GenSpawn.Spawn(wave, center.ToIntVec3(), caster.Map);

            for (int i = 0; i < ShockwaveParticles; i++)
            {
                Thing_RoyalFlareShockwaveParticle p = (Thing_RoyalFlareShockwaveParticle)ThingMaker.MakeThing(ThingDef.Named("RoyalFlareShockwaveParticle"));
                p.Init(center);
                GenSpawn.Spawn(p, center.ToIntVec3(), caster.Map);
            }
        }

        private void SpawnConeLights()
        {
            if (conesSpawned || caster.Map == null) return;
            conesSpawned = true;
            coneLights = new Thing_RoyalFlareConeLight[3];
            for (int i = 0; i < 3; i++)
            {
                Thing_RoyalFlareConeLight cone = (Thing_RoyalFlareConeLight)ThingMaker.MakeThing(ThingDef.Named("RoyalFlareConeLight"));
                cone.caster = caster;
                cone.parentEmitter = this;
                cone.angleOffset = i * 120f;
                GenSpawn.Spawn(cone, SunDrawPos.ToIntVec3(), caster.Map);
                coneLights[i] = cone;
            }
        }

        private void SpawnReverseConeLights()
        {
            if (reverseConesSpawned || caster.Map == null) return;
            reverseConesSpawned = true;
            reverseConeLights = new Thing_RoyalFlareConeLightReverse[3];
            for (int i = 0; i < 3; i++)
            {
                Thing_RoyalFlareConeLightReverse cone = (Thing_RoyalFlareConeLightReverse)ThingMaker.MakeThing(ThingDef.Named("RoyalFlareConeLightReverse"));
                cone.caster = caster;
                cone.parentEmitter = this;
                cone.angleOffset = i * 120f;
                GenSpawn.Spawn(cone, SunDrawPos.ToIntVec3(), caster.Map);
                reverseConeLights[i] = cone;
            }
        }

        private void Cleanup()
        {
            if (caster != null && !caster.Dead && caster.health != null)
            {
                Hediff h = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(LockHediffName));
                if (h != null) caster.health.RemoveHediff(h);
            }
            if (coneLights != null) foreach (var c in coneLights) if (c != null && !c.Destroyed) c.Destroy();
            if (reverseConeLights != null) foreach (var c in reverseConeLights) if (c != null && !c.Destroyed) c.Destroy();
            if (rangeIndicator != null && !rangeIndicator.Destroyed) rangeIndicator.Destroy();
            if (!Destroyed) Destroy();
        }
    }
}