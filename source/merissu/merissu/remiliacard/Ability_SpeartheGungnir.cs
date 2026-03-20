using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound; 

namespace merissu
{
    public class Ability_SpeartheGungnir : Ability
    {
        public Ability_SpeartheGungnir() : base() { }

        public Ability_SpeartheGungnir(Pawn pawn, AbilityDef def)
            : base(pawn, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                Hediff hp = pawn.health.hediffSet
                    .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

                if (hp == null || hp.Severity < 1f)
                    return "灵力不足";

                return AcceptanceReport.WasAccepted;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.IsValid || pawn.Map == null)
                return false;

            Hediff hp = pawn.health.hediffSet
                .GetFirstHediffOfDef(HediffDef.Named("FullPower"));

            if (hp == null || hp.Severity < 1f)
                return false;

            hp.Severity -= 1f;

            SoundDef gungnirSound = SoundDef.Named("Gungnir");
            if (gungnirSound != null)
            {
                gungnirSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }

            Thing thing = GenSpawn.Spawn(
                ThingDef.Named("Thing_GungnirProjectile"),
                pawn.Position,
                pawn.Map);

            Thing_GungnirProjectile proj = thing as Thing_GungnirProjectile;

            Vector3 dir = (target.Cell.ToVector3Shifted() - pawn.DrawPos).normalized;

            proj.Initialize(pawn, dir);

            return true;
        }
    }

    public class Thing_GungnirProjectile : Thing
    {
        private Pawn launcher;
        private Vector3 direction;
        private Vector3 exactPos;

        private float speed = 1.5f;
        private int age;

        private const int TotalFramesA = 34;
        private const int TicksPerFrame = 2;
        private const int TotalFramesB = 15;

        private const float CollisionRadius = 2.5f;
        private HashSet<Thing> alreadyHit = new HashSet<Thing>();

        public void Initialize(Pawn launcher, Vector3 dir)
        {
            this.launcher = launcher;
            this.direction = dir.normalized;
            this.exactPos = launcher.DrawPos;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null) return;

            age++;
            exactPos += direction * speed;
            IntVec3 centerCell = exactPos.ToIntVec3();

            if (!centerCell.InBounds(Map))
            {
                Destroy();
                return;
            }

            Position = centerCell;

            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(centerCell, CollisionRadius, true);

            foreach (IntVec3 c in cells)
            {
                if (!c.InBounds(Map)) continue;

                List<Thing> thingList = c.GetThingList(Map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];

                    if (t == this || t == launcher) continue;
                    if (alreadyHit.Contains(t)) continue;
                    if (t.def.category == ThingCategory.Item) continue;

                    alreadyHit.Add(t);

                    if (t is Pawn pawn)
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            if (pawn.Dead) break;

                            DamageInfo dinfo = new DamageInfo(
                                DamageDefOf.Stab,
                                100,
                                999f,
                                -1f,
                                launcher);

                            pawn.TakeDamage(dinfo);
                        }
                    }
                    else
                    {
                        DamageInfo dinfo = new DamageInfo(
                            DamageDefOf.Blunt,
                            500,
                            999f,
                            -1f,
                            launcher);

                        t.TakeDamage(dinfo);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float angle = direction.AngleFlat();
            float offset = -90f;
            Quaternion rot = Quaternion.AngleAxis(angle + offset, Vector3.up);
            Vector3 drawPos = exactPos;

            int frameA = Mathf.Min(age / TicksPerFrame, TotalFramesA - 1);
            string pathA = "Projectiles/Gungnir/spellBulletBa" + frameA.ToString("D3");
            Material matA = MaterialPool.MatFrom(pathA, ShaderDatabase.MoteGlow);

            if (matA != null)
            {
                Vector3 posA = drawPos;
                posA.y = AltitudeLayer.Projectile.AltitudeFor();
                Matrix4x4 matrixA = Matrix4x4.TRS(posA, rot, new Vector3(20f, 1f, 20f));
                Graphics.DrawMesh(MeshPool.plane10, matrixA, matA, 0);
            }

            int frameB = (age / TicksPerFrame) % TotalFramesB;
            string pathB = "Projectiles/Spear/spellBulletBb" + frameB.ToString("D3");
            Material matB = MaterialPool.MatFrom(pathB, ShaderDatabase.MoteGlow);

            if (matB != null)
            {
                float shiftBack = 4.0f;
                Vector3 posB = drawPos - (direction * shiftBack);
                posB.y = AltitudeLayer.Projectile.AltitudeFor() + 0.01f;

                Matrix4x4 matrixB = Matrix4x4.TRS(posB, rot, new Vector3(10f, 1f, 5f));
                Graphics.DrawMesh(MeshPool.plane10, matrixB, matB, 0);
            }
        }
    }
}