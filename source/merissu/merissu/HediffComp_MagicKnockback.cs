using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class HediffCompProperties_MagicKnockback : HediffCompProperties
    {
        public float radius = 5.9f;
        public float knockbackDistance = 7f;
        public int stunTicks = 180;
        public string flyerDefName = "PawnFlyer"; 

        public HediffCompProperties_MagicKnockback()
        {
            compClass = typeof(HediffComp_MagicKnockback);
        }
    }

    public class HediffComp_MagicKnockback : HediffComp
    {
        public HediffCompProperties_MagicKnockback Props => (HediffCompProperties_MagicKnockback)props;

        private float lastSeverity = -1f;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            TriggerKnockback();
            lastSeverity = parent.Severity;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (lastSeverity < 0)
            {
                lastSeverity = parent.Severity;
                return;
            }

            if (parent.Severity > lastSeverity)
            {
                TriggerKnockback();
            }

            lastSeverity = parent.Severity;
        }

        private void TriggerKnockback()
        {
            Pawn caster = base.Pawn;
            Map map = caster.Map;
            if (map == null) return;

            FleckDef ringDef = DefDatabase<FleckDef>.GetNamed("Merissu_MagicRing", false);
            if (ringDef != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    FleckCreationData data = FleckMaker.GetDataStatic(caster.TrueCenter(), map, ringDef);
                    data.rotation = Rand.Range(0f, 360f); 
                    data.rotationRate = 0f;               
                    data.scale = 1.0f;                    
                    map.flecks.CreateFleck(data);
                }
            }

            List<Thing> targets = GenRadial.RadialDistinctThingsAround(caster.Position, map, Props.radius, true).ToList();

            foreach (Thing thing in targets)
            {
                if (thing is Pawn victim && victim != caster && !victim.Dead)
                {
                    if (victim.Faction == null || victim.Faction.HostileTo(caster.Faction))
                    {
                        ApplyPhysicalKnockback(caster, victim);
                    }
                }
            }
        }
        private void ApplyPhysicalKnockback(Pawn caster, Pawn victim)
        {
            Map map = victim.Map;
            Vector3 direction = (victim.Position - caster.Position).ToVector3();
            if (direction == Vector3.zero) direction = Vector3.forward;
            direction.Normalize();

            IntVec3 targetCell = (victim.Position.ToVector3() + direction * Props.knockbackDistance).ToIntVec3();

            if (!targetCell.InBounds(map) || !targetCell.Walkable(map))
            {
                if (!CellFinder.TryFindRandomCellNear(targetCell, map, 2, c => c.Walkable(map), out targetCell))
                {
                    targetCell = victim.Position;
                }
            }

            if (targetCell != victim.Position)
            {
                ThingDef fDef = DefDatabase<ThingDef>.GetNamed(Props.flyerDefName, false);
                if (fDef != null)
                {
                    PawnFlyer flyer = PawnFlyer.MakeFlyer(
                        fDef,
                        victim,
                        targetCell,
                        null,
                        null,
                        false,
                        null,
                        null,
                        LocalTargetInfo.Invalid
                    );

                    if (flyer != null)
                    {
                        GenSpawn.Spawn(flyer, targetCell, map);
                    }
                }
                else
                {
                    victim.Position = targetCell;
                }
            }

            if (Props.stunTicks > 0 && victim.stances?.stunner != null)
            {
                victim.stances.stunner.StunFor(Props.stunTicks, caster, false, false);
            }

            victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 2f, 0, -1, caster));
        }
    }
}