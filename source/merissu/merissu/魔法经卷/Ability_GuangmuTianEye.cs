using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Ability_GuangmuTianEye : Ability
    {
        private static readonly Type ProjectileCEType = AccessTools.TypeByName("CombatExtended.ProjectileCE");

        public Ability_GuangmuTianEye() : base() { }
        public Ability_GuangmuTianEye(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override AcceptanceReport CanCast => true;

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.Activate(target, dest)) return false;
            if (pawn == null || pawn.Map == null) return false;

            Find.CameraDriver.shaker.DoShake(2f);
            Map map = pawn.Map;

            ClearProjectilesInRange(map, 20f);

            IEnumerable<Thing> targets = GenRadial.RadialDistinctThingsAround(pawn.Position, map, 20f, true);
            foreach (Thing t in targets)
            {
                if (t is Pawn victim && victim != pawn && !victim.Dead &&
                    victim.Faction != null && victim.Faction.HostileTo(pawn.Faction))
                {
                    victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 20f, 0f, -1f, pawn));
                    if (!victim.Dead && !victim.Destroyed && victim.Spawned)
                        DoKnockback(victim, map);
                }
            }

            SpawnRing(ThingDef.Named("GuangmuTianEyeRingA"));
            SpawnRing(ThingDef.Named("GuangmuTianEyeRingB"));
            SpawnRing(ThingDef.Named("GuangmuTianEyeRingC"));

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ShinkiBuff_Regen");
            if (buffDef != null)
            {
                Hediff defenseBuff = pawn.health.hediffSet.GetFirstHediffOfDef(buffDef);
                if (defenseBuff != null)
                {
                    HediffDef hijiriDef = DefDatabase<HediffDef>.GetNamedSilentFail("hijiriShinkiRecitation");
                    if (hijiriDef == null || pawn.health.hediffSet.GetFirstHediffOfDef(hijiriDef) == null)
                    {
                        pawn.health.RemoveHediff(defenseBuff);
                    }
                }
            }

            return true;
        }

        private void ClearProjectilesInRange(Map map, float radius)
        {
            IEnumerable<Thing> things = GenRadial.RadialDistinctThingsAround(pawn.Position, map, radius, true);
            foreach (Thing t in things)
            {
                if (t is Projectile || (ProjectileCEType != null && ProjectileCEType.IsAssignableFrom(t.GetType())))
                {
                    FleckMaker.ThrowMicroSparks(t.DrawPos, map);
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private void SpawnRing(ThingDef def)
        {
            Thing ring = ThingMaker.MakeThing(def);
            if (ring is Thing_GuangmuTianEyeRing eyeRing)
            {
                eyeRing.caster = pawn;
                GenSpawn.Spawn(eyeRing, pawn.Position, pawn.Map);
            }
        }

        private void DoKnockback(Pawn victim, Map map)
        {
            Vector3 knockDir = (victim.DrawPos - pawn.DrawPos).normalized;
            if (knockDir.sqrMagnitude < 0.01f)
                knockDir = Vector3.forward;

            Vector3 destVec = victim.DrawPos + knockDir * 20f;
            IntVec3 destCell = destVec.ToIntVec3();
            if (!destCell.InBounds(map))
                destCell = CellFinder.RandomClosewalkCellNear(destCell, map, 1);
            if (!destCell.IsValid || !destCell.InBounds(map)) return;

            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                ThingDef.Named("PawnFlyer"), victim, destCell, null, null);
            if (flyer != null)
                GenSpawn.Spawn(flyer, destCell, map);
        }
    }
}