using RimWorld;
using UnityEngine;
using Verse;

namespace merissu
{
    public class Ability_YinYangFlyingBirdWell : Ability
    {
        public Ability_YinYangFlyingBirdWell() : base() { }

        public Ability_YinYangFlyingBirdWell(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn == null || pawn.Map == null)
                return false;

            Pawn caster = pawn;
            Map map = pawn.Map;
            HediffDef hDef = HediffDef.Named("ReimuCardDeclared");
            if (hDef != null)
            {
                Hediff firstHediffOfDef = caster.health.hediffSet.GetFirstHediffOfDef(hDef);
                if (firstHediffOfDef != null)
                {
                    caster.health.RemoveHediff(firstHediffOfDef);
                }
            }

            HediffDef hDefSeal = HediffDef.Named("YinYangOrb");
            if (hDefSeal != null)
            {
                caster.health.AddHediff(hDefSeal, null, null);
            }
            ThingDef orbDef = DefDatabase<ThingDef>.GetNamed("YinYangOrb_Big");

            IntVec3 spawnPos = FindSafeSpawnCell(pawn, map);

            Thing orb = ThingMaker.MakeThing(orbDef);
            GenSpawn.Spawn(orb, spawnPos, map);

            orb.TryGetComp<Comp_YinYangOrb>()?.Init(target.Cell, pawn);

            return true;
        }

        private IntVec3 FindSafeSpawnCell(Pawn pawn, Map map)
        {
            IntVec3 forward = pawn.Rotation.FacingCell;

            IntVec3 cell1 = pawn.Position + forward * 3;
            if (IsCellUsable(cell1, map))
                return cell1;

            IntVec3 cell2 = pawn.Position + forward * 4;
            if (IsCellUsable(cell2, map))
                return cell2;

            IntVec3 left = pawn.Position + pawn.Rotation.Rotated(RotationDirection.Counterclockwise).FacingCell;
            if (IsCellUsable(left, map))
                return left;

            IntVec3 right = pawn.Position + pawn.Rotation.Rotated(RotationDirection.Clockwise).FacingCell;
            if (IsCellUsable(right, map))
                return right;

            return pawn.Position;
        }

        private bool IsCellUsable(IntVec3 cell, Map map)
        {
            return cell.InBounds(map)
                && !cell.Filled(map)
                && cell.Walkable(map);
        }
    }
}
