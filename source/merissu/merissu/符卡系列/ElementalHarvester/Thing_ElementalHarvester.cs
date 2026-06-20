using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_ElementalHarvester : Thing
    {
        private Pawn caster;
        private float angle;
        private int age;
        private float angleSlow;
        private float angleThird;
        private float angleFourth;
        private int damageTick;

        private const int DurationTicks = 600;
        private const float Radius = 6f;
        private const int DamageInterval = 6;

        public void Init(Pawn pawn)
        {
            caster = pawn;
            angle = 0f;
            angleThird = 0f;
            angleFourth = 0f;
        }
        private static readonly Mesh HarvesterMesh =
           MeshPool.plane10;

        private static readonly Material HarvesterMat =
            MaterialPool.MatFrom("Projectiles/ElementalSaw_CW",ShaderDatabase.Transparent);

        private static readonly Material HarvesterMat_ThirdSource = 
            MaterialPool.MatFrom("Other/ElementalSaw_CW_Fade", ShaderDatabase.Transparent);

        private static readonly Material HarvesterMat_Secondary = 
            MaterialPool.MatFrom("Projectiles/ElementalSaw_CCW",ShaderDatabase.Transparent);

        private static readonly Material HarvesterMat_Fourth =
            MaterialPool.MatFrom("Other/ElementalSaw_CCW_Fade", ShaderDatabase.Transparent);
        protected override void Tick()
        {
            base.Tick();

            if (caster == null ||
                caster.Destroyed ||
                caster.Dead ||
                caster.Map != Map)
            {
                Destroy();
                return;
            }

            if (Position != caster.Position)
            {
                Position = caster.Position;
            }

            age++;
            damageTick++;

            angle -= 15f;
            angleSlow += 5f;
            angleThird -= 20f;
            angleFourth += 20f;

            if (damageTick >= DamageInterval)
            {
                DoDamage();
                damageTick = 0;
            }

            if (age >= DurationTicks)
            {
                Destroy();
            }
        }

        private void DoDamage()
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(
                caster.Position,
                Radius,
                true))
            {
                if (!cell.InBounds(Map))
                    continue;

                List<Thing> things = cell.GetThingList(Map);

                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];

                    if (thing == caster)
                        continue;

                    if (thing.Destroyed)
                        continue;

                    if (!(thing is Pawn) &&
                        !(thing is Building) &&
                        !(thing is Plant))
                        continue;

                    if (thing is Pawn pawn)
                    {
                        if (pawn.Dead)
                            continue;

                        if (!pawn.HostileTo(caster))
                            continue;
                    }

                    thing.TakeDamage(new DamageInfo(
                        DamageDefOf.Cut,
                        8f,
                        0f,
                        angle,
                        caster));
                }
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null) return;

            Vector3 center = caster.DrawPos;
            float size = Radius * 2f;
            Vector3 scale = new Vector3(size, 1f, size);

            float y1 = AltitudeLayer.Pawn.AltitudeFor() - 0.2f;
            Matrix4x4 matrix1 = Matrix4x4.TRS(
                new Vector3(center.x, y1, center.z),
                Quaternion.Euler(0f, angle, 0f),
                scale);
            Graphics.DrawMesh(HarvesterMesh, matrix1, HarvesterMat, 0);

            float y2 = AltitudeLayer.Pawn.AltitudeFor() - 0.19f;
            Matrix4x4 matrix2 = Matrix4x4.TRS(
                new Vector3(center.x, y2, center.z),
                Quaternion.Euler(0f, angleSlow, 0f),
                scale);
            Graphics.DrawMesh(HarvesterMesh, matrix2, HarvesterMat_Secondary, 0);

            float y3 = AltitudeLayer.Pawn.AltitudeFor() - 0.21f;
            Matrix4x4 matrix3 = Matrix4x4.TRS(
                new Vector3(center.x, y3, center.z),
                Quaternion.Euler(0f, angleThird, 0f),
                scale);
            Graphics.DrawMesh(HarvesterMesh, matrix3, HarvesterMat_ThirdSource, 0);

            float y4 = AltitudeLayer.Pawn.AltitudeFor() - 0.21f;
            Matrix4x4 matrix4 = Matrix4x4.TRS(
                new Vector3(center.x, y4, center.z),
                Quaternion.Euler(0f, angleFourth, 0f), 
                scale);
            Graphics.DrawMesh(HarvesterMesh, matrix4, HarvesterMat_Fourth, 0);
        }
    }
}
