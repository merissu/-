using RimWorld;
using Verse;
using UnityEngine;

namespace merissu
{
    public class CompProperties_AbilityTeleportBackstab : CompProperties_AbilityEffect
    {
        public float damageAmount = 54f;

        public CompProperties_AbilityTeleportBackstab()
        {
            compClass = typeof(CompAbilityEffect_TeleportBackstab);
        }
    }


    public class CompAbilityEffect_TeleportBackstab : CompAbilityEffect
    {
        public new CompProperties_AbilityTeleportBackstab Props =>
            (CompProperties_AbilityTeleportBackstab)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;

            if (caster == null || victim == null || victim.Dead)
                return;

            Map map = caster.Map;

            FleckMaker.ThrowSmoke(caster.Position.ToVector3Shifted(), map, 1.5f);

            IntVec3 backCell = GetBackCell(victim, map);

            if (backCell.IsValid && backCell.Walkable(map))
            {
                caster.Position = backCell;
                caster.Notify_Teleported();
            }

            FleckMaker.ThrowSmoke(caster.Position.ToVector3Shifted(), map, 1.2f);
            FleckMaker.ThrowMicroSparks(caster.Position.ToVector3Shifted(), map);

            caster.rotationTracker.FaceTarget(victim);

            BodyPartRecord part = GetAssassinationPart(victim);

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Stab,
                Props.damageAmount,
                armorPenetration: 999f,
                instigator: caster,
                hitPart: part
            );

            victim.TakeDamage(dinfo);
        }
        private IntVec3 GetBackCell(Pawn victim, Map map)
        {
            IntVec3 behind = victim.Position - victim.Rotation.FacingCell;

            if (!behind.InBounds(map) || !behind.Walkable(map))
                return victim.Position;

            return behind;
        }
        private BodyPartRecord GetAssassinationPart(Pawn pawn)
        {
            if (pawn.RaceProps?.body == null)
                return null;

            BodyPartRecord heart = null;
            BodyPartRecord neck = null;
            BodyPartRecord brain = null;
            BodyPartRecord torso = null;

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part.def == BodyPartDefOf.Heart)
                    heart = part;
                else if (part.def == BodyPartDefOf.Neck)
                    neck = part;
                else if (part.def == BodyPartDefOf.Torso)
                    torso = part;
            }

            if (heart != null) return heart;
            if (neck != null) return neck;
            if (brain != null) return brain;
            return torso;
        }
    }
}
