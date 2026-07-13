using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class AttackMode_VigilanceFormation : GoheiAttackMode
    {
        public override string ModeName => "VigilanceFormation";
        protected override string ProjectileDefName => "REIMU_SpinningTalisman";
        protected override string SoundDefName => "VigilanceFormation";
        public override int BurstCount => 1;
        public override int TicksBetweenShots => 0;
        public override float WarmupTime => 1f;

        private const float FormationWidth = 1.5f;
        private const float FormationHeight = 4f;
        private const float RotationOffset = 90f;

        public override bool OverrideCastShot(Verb_GoheiRandomShoot verb, LocalTargetInfo target)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster.Map;
            if (map == null) return false;

            SoundDef.Named(SoundDefName)?.PlayOneShot(new TargetInfo(caster.Position, map));

            Vector3 aimDir;
            if (target.IsValid && target.Cell != caster.Position)
                aimDir = (target.Cell.ToVector3Shifted() - caster.DrawPos).normalized;
            else
                aimDir = caster.Rotation.FacingCell.ToVector3().normalized;

            Vector3 center = caster.DrawPos + aimDir * 3f;

            Quaternion baseRot = Quaternion.LookRotation(-aimDir);
            Quaternion extraRot = Quaternion.Euler(0f, RotationOffset, 0f);
            Quaternion totalRot = baseRot * extraRot;
            Vector3 formationForward = totalRot * Vector3.forward; 
            Vector3 formationRight = totalRot * Vector3.right;     

            float halfH = FormationHeight / 2f;
            float halfW = FormationWidth / 2f;

            Vector3[] corners = new Vector3[4]
            {
                center + formationForward * halfH + formationRight * halfW,
                center + formationForward * halfH - formationRight * halfW,
                center - formationForward * halfH + formationRight * halfW,
                center - formationForward * halfH - formationRight * halfW
            };

            foreach (Vector3 corner in corners)
            {
                ThingDef orbDef = ThingDef.Named("Mote_VigilanceOrb");
                Mote_VigilanceOrb orb = (Mote_VigilanceOrb)ThingMaker.MakeThing(orbDef);
                orb.startPos = caster.DrawPos;
                orb.endPos = corner;
                orb.launcher = caster;
                GenSpawn.Spawn(orb, corner.ToIntVec3(), map);
            }

            ThingDef spawnerDef = ThingDef.Named("DelayedFormationSpawner");
            DelayedFormationSpawner spawner = (DelayedFormationSpawner)ThingMaker.MakeThing(spawnerDef);
            spawner.center = center;
            spawner.aimDirection = aimDir;
            spawner.faction = caster.Faction;
            GenSpawn.Spawn(spawner, center.ToIntVec3(), map);

            return true;
        }
    }
}