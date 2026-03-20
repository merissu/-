using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    public class SatelliteThing : Thing
    {
        public Pawn caster;
        public Vector3 fixedOffset;
        public float finalRotation;
        public bool drawLightSphere = false; 

        private int lifeTicks = 1;
        private Graphic lightSphereGraphic;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (this.def.defName == "SatelliteThing_New")
            {
                lifeTicks = 3; 
                lightSphereGraphic = GraphicDatabase.Get<Graphic_Single>(
                    "Other/spellBulletEc001",
                    ShaderDatabase.Transparent,
                    new Vector2(2.5f, 2.5f),
                    Color.white
                );
            }
            else
            {
                lifeTicks = 3; 
                lightSphereGraphic = GraphicDatabase.Get<Graphic_Single>(
                    "Other/spellBulletEc000",
                    ShaderDatabase.Transparent,
                    new Vector2(2.5f, 2.5f),
                    Color.white
                );
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (caster != null && caster.Spawned && !caster.Dead)
            {
                IntVec3 targetPos = (caster.DrawPos + fixedOffset).ToIntVec3();
                if (this.Position != targetPos) this.Position = targetPos;

                lifeTicks--;
                if (lifeTicks <= 0) SpawnProjectileAndDestroy();
            }
            else if (!Destroyed) { this.Destroy(); }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null || this.Graphic == null) return;

            Vector3 drawPos = caster.DrawPos + fixedOffset;
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(finalRotation, Vector3.up),
                this.def.graphicData.drawSize.ToVector3()
            );
            Graphics.DrawMesh(MeshPool.plane10, matrix, this.Graphic.MatSingle, 0);

            if (drawLightSphere && lightSphereGraphic != null)
            {
                Vector3 lightPos = drawPos;
                lightPos.y += 0.01f;

                Matrix4x4 lightMatrix = Matrix4x4.TRS(
                    lightPos,
                    Quaternion.identity,
                    lightSphereGraphic.drawSize.ToVector3()
                );
                Graphics.DrawMesh(MeshPool.plane10, lightMatrix, lightSphereGraphic.MatSingle, 0);
            }
        }

        private void SpawnProjectileAndDestroy()
        {
            if (this.Destroyed) return;
            Vector3 exactSpawnPos = this.DrawPos;
            float savedRotation = this.finalRotation;
            Map map = this.Map;
            Pawn tempCaster = this.caster;

            this.Destroy();

            if (this.def.defName == "SatelliteThing")
            {
                Fire("SatelliteThingtrue", exactSpawnPos, savedRotation, map, tempCaster);
                Fire("twoSatelliteThingtrue", exactSpawnPos, savedRotation, map, tempCaster);
                Fire("threeSatelliteThingtrue", exactSpawnPos, savedRotation, map, tempCaster);
            }
            else
            {
                Fire("greenSatelliteThingtrue", exactSpawnPos, savedRotation, map, tempCaster);
            }
        }

        private void Fire(string defName, Vector3 pos, float rot, Map map, Pawn launcher)
        {
            ThingDef pDef = ThingDef.Named(defName);
            if (pDef == null) return;
            Projectile p = (Projectile)GenSpawn.Spawn(pDef, pos.ToIntVec3(), map);
            if (p != null)
            {
                Vector3 targetVec = pos + Quaternion.AngleAxis(rot, Vector3.up) * Vector3.forward * 50f;
                p.Launch(launcher, pos, targetVec.ToIntVec3(), targetVec.ToIntVec3(), ProjectileHitFlags.All, false, null);
                p.DrawPos.Set(pos.x, pos.y, pos.z);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref fixedOffset, "fixedOffset");
            Scribe_Values.Look(ref finalRotation, "finalRotation");
            Scribe_Values.Look(ref lifeTicks, "lifeTicks");
            Scribe_Values.Look(ref drawLightSphere, "drawLightSphere");
        }
    }
}