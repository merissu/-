using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public static class PenetratingBulletGraphics
    {
        public static readonly Material UnderlayMat = MaterialPool.MatFrom(
            "Projectiles/REIMU/Talisman/tailAa003", ShaderDatabase.MoteGlow);

        private static Mesh _underlayMesh;
        public static Mesh UnderlayMesh
        {
            get
            {
                if (_underlayMesh == null)
                {
                    _underlayMesh = new Mesh();
                    _underlayMesh.name = "UnderlayMesh";
                    _underlayMesh.vertices = new Vector3[]
                    {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f,  0.5f),
                    new Vector3( 0.5f, 0f,  0.5f),
                    new Vector3( 0.5f, 0f, -0.5f),
                    };
                    _underlayMesh.uv = new Vector2[]
                    {
                    new Vector2(0.005f, 0.005f),
                    new Vector2(0.005f, 0.995f),
                    new Vector2(0.995f, 0.995f),
                    new Vector2(0.995f, 0.005f),
                    };
                    _underlayMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                    _underlayMesh.RecalculateNormals();
                    _underlayMesh.RecalculateBounds();
                }
                return _underlayMesh;
            }
        }
    }

    public class Projectile_PenetratingFrameDamage : Projectile
    {
        private Vector3 currentRealPos = Vector3.zero;
        private Vector3 velocity = Vector3.zero;

        public override Vector3 DrawPos => currentRealPos;

        public override Quaternion ExactRotation
        {
            get
            {
                if (velocity.sqrMagnitude > 0.0001f)
                    return Quaternion.LookRotation(velocity);
                return Quaternion.identity;
            }
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventDrawing = false,
            Thing equipment = null, ThingDef thingDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventDrawing, equipment, thingDef);
            currentRealPos = origin;
            velocity = (intendedTarget.CenterVector3 - origin).normalized;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Map == null) return;

            Vector3 underlayPos = drawLoc - velocity * 2f;   
            underlayPos.y = def.altitudeLayer.AltitudeFor();

            Matrix4x4 underlayMatrix = Matrix4x4.TRS(
                underlayPos,
                ExactRotation,
                new Vector3(4f, 1f, 4f)
            );
            Graphics.DrawMesh(PenetratingBulletGraphics.UnderlayMesh, underlayMatrix, PenetratingBulletGraphics.UnderlayMat, 0);

            base.DrawAt(drawLoc, flip);
        }
        protected override void Tick()
        {
            base.Tick();
            if (Map == null)
            {
                Destroy();
                return;
            }

            float step = def.projectile.speed / 100f;
            Vector3 nextPos = currentRealPos + velocity * step;
            if (!nextPos.InBounds(Map))
            {
                Destroy();
                return;
            }
            currentRealPos = nextPos;
            Position = currentRealPos.ToIntVec3();

            DealFrameDamageToAllInCell();

            if (IsHittingBuilding())
                Destroy();
        }

        private void DealFrameDamageToAllInCell()
        {
            if (Map == null || launcher == null) return;
            List<Thing> things = Map.thingGrid.ThingsListAt(Position);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing t = things[i];
                if (t is Pawn pawn && !pawn.Dead && pawn.Faction != null
                    && launcher.Faction != null && pawn.Faction.HostileTo(launcher.Faction))
                {
                    float dmg = def.projectile.GetDamageAmount(launcher, null);
                    float pen = def.projectile.GetArmorPenetration(launcher, null);
                    DamageDef dd = def.projectile.damageDef ?? DamageDefOf.Bullet;
                    DamageInfo dinfo = new DamageInfo(dd, dmg, pen, ExactRotation.eulerAngles.y,
                        launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                    pawn.TakeDamage(dinfo);
                }
            }
        }

        private bool IsHittingBuilding()
        {
            if (Map == null) return false;
            Building building = Position.GetEdifice(Map);
            return building != null && building.def.passability == Traversability.Impassable;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false) { }
    }
}