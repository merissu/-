using RimWorld;
using System.Linq;
using Verse;
using UnityEngine;

namespace merissu
{
    public class Comp_YinYangOrb : ThingComp
    {
        private IntVec3 targetCell;
        private Pawn caster;

        private Vector3 visualPos;
        private Vector3 moveDir;

        private float rotationAngle;

        private bool arrived;
        private int lingerLeft;

        public Vector3 VisualPos => visualPos;
        public float RotationAngle => rotationAngle;

        public CompProperties_YinYangOrb Props =>
            (CompProperties_YinYangOrb)props;

        public void Init(IntVec3 target, Pawn caster)
        {
            this.targetCell = target;
            this.caster = caster;

            visualPos = parent.Position.ToVector3Shifted();
            moveDir = (target.ToVector3Shifted() - visualPos).normalized;

            rotationAngle = Rand.Range(0f, 360f);
        }

        public override void CompTick()
        {
            base.CompTick();

            rotationAngle += Props.rotateSpeed;
            if (rotationAngle > 360f) rotationAngle -= 360f;

            DamageInRadius();

            if (!arrived)
            {
                visualPos += moveDir * Props.moveSpeed;

                IntVec3 newCell = visualPos.ToIntVec3();
                if (newCell != parent.Position && newCell.InBounds(parent.Map))
                {
                    parent.Position = newCell;
                }

                if (parent.Position == targetCell)
                {
                    arrived = true;
                    lingerLeft = Props.lingerTicks;
                }
            }
            else
            {
                lingerLeft--;
                if (lingerLeft <= 0)
                {
                    Explode();
                }
            }
        }

        private void DamageInRadius()
        {
            var things = parent.Map.listerThings.AllThings
                .Where(t =>
                    t != parent &&
                    !t.Destroyed &&
                    t.Position.DistanceTo(parent.Position) <= Props.radius
                )
                .ToList();

            foreach (Thing t in things)
            {
                if (t == caster) continue;

                if (t is Pawn pawn)
                {
                    pawn.TakeDamage(new DamageInfo(
                        DamageDefOf.Blunt,
                        Props.damagePerTick,
                        instigator: caster
                    ));
                }
                else if (t.def.category == ThingCategory.Building ||
                         t.def.category == ThingCategory.Item)
                {
                    t.TakeDamage(new DamageInfo(
                        DamageDefOf.Blunt,
                        Props.damagePerTick
                    ));
                }
            }
        }

        private void Explode()
        {
            GenExplosion.DoExplosion(
                parent.Position,
                parent.Map,
                Props.explosionRadius,
                DamageDefOf.Bomb,
                caster,
                Props.explosionDamage
            );

            parent.Destroy();
        }
    }
}
