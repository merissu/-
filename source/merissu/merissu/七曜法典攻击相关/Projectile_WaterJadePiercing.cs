using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static UnityEngine.UI.Image;

namespace merissu
{
    public class Projectile_WaterJadePiercing : Projectile
    {
        private float angle0 = 0f;
        private float angle1 = 0f;

        private HashSet<Thing> damagedPawns = new HashSet<Thing>();

        private static Material mat0;
        private static Material mat1;
        private static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

        private void InitMaterialsIfNeed()
        {
            if (mat0 == null) mat0 = MaterialPool.MatFrom("Projectiles/waterjade/bulletAb000", ShaderDatabase.MoteGlow);
            if (mat1 == null) mat1 = MaterialPool.MatFrom("Projectiles/waterjade/bulletAb001", ShaderDatabase.MoteGlow);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null || !this.Spawned) return;

            angle0 += 12f; 
            angle1 -= 12f; 
            if (angle0 >= 360f) angle0 -= 360f;
            if (angle1 <= -360f) angle1 += 360f;

            if (this.IsHashIntervalTick(2))
            {
                DoPiercingDamageAndExtinguish();
            }
        }

        private void DoPiercingDamageAndExtinguish()
        {
            var list = GenRadial.RadialDistinctThingsAround(Position, Map, 1.2f, true);

            foreach (Thing t in list)
            {
                if (t is Pawn p && !p.Dead)
                {
                    if (p.Faction != null && p.HostileTo(launcher?.Faction) && !damagedPawns.Contains(p))
                    {
                        damagedPawns.Add(p);
                        DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, this.def.projectile.damageAmountBase, this.def.projectile.armorPenetrationBase, this.ExactRotation.eulerAngles.y, launcher);
                        p.TakeDamage(dinfo);
                    }
                }
                else if (t is Fire fire)
                {
                    fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                }
                else if (t.IsBurning())
                {
                    t.TryGetComp<CompAttachBase>()?.GetAttachment(ThingDefOf.Fire)?.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100f));
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            InitMaterialsIfNeed();

            float totalDist = (destination - origin).magnitude;
            float currentDist = (ExactPosition - origin).magnitude;

            float t = Mathf.Clamp01(currentDist / totalDist);
            float alpha = 1f;
            if (t > 0.6f) 
            {
                alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            }
            matPropertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, alpha));

            Vector3 size = new Vector3(2f, 1f, 2f);
            Vector3 drawPos = ExactPosition;
            drawPos.y = AltitudeLayer.Projectile.AltitudeFor();

            Matrix4x4 matrix0 = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, angle0, 0), size);
            Graphics.DrawMesh(MeshPool.plane10, matrix0, mat0, 0, null, 0, matPropertyBlock);

            Matrix4x4 matrix1 = Matrix4x4.TRS(drawPos, Quaternion.Euler(0, angle1, 0), size);
            Graphics.DrawMesh(MeshPool.plane10, matrix1, mat1, 0, null, 0, matPropertyBlock);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);
        }
    }
}