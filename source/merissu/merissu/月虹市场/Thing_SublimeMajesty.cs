using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Thing_SublimeMajesty : Thing
    {
        public Pawn caster;
        private int age = 0;
        private const int DurationTicks = 180;
        private const float Radius = 30f;

        private const int LaserCount = 60;
        private List<LaserData> lasers = new List<LaserData>();

        private static readonly Material LaserMat = MaterialPool.MatFrom("Other/bulletCe000", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        private class LaserData : IExposable
        {
            public float startAngle;
            public float rotateSpeed;
            public int fadeInDelay;
            public int fadeOutDelay;

            public LaserData() { }

            public void ExposeData()
            {
                Scribe_Values.Look(ref startAngle, "startAngle");
                Scribe_Values.Look(ref rotateSpeed, "rotateSpeed");
                Scribe_Values.Look(ref fadeInDelay, "fadeInDelay");
                Scribe_Values.Look(ref fadeOutDelay, "fadeOutDelay");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Collections.Look(ref lasers, "lasers", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (lasers == null) lasers = new List<LaserData>();
                if (lasers.Count == 0) InitLasers();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad || lasers == null || lasers.Count == 0)
            {
                InitLasers();
            }
        }

        private void InitLasers()
        {
            lasers.Clear();
            for (int i = 0; i < LaserCount; i++)
            {
                float speed = Rand.Range(1f, 5.0f);
                if (Rand.Value > 0.5f) speed *= -1f;
                lasers.Add(new LaserData
                {
                    startAngle = Rand.Range(0f, 360f),
                    rotateSpeed = speed,
                    fadeInDelay = Rand.Range(0, 30),
                    fadeOutDelay = Rand.Range(0, 30)
                });
            }
        }

        protected override void Tick()
        {
            if (caster == null || !caster.Spawned || caster.Map == null || age >= DurationTicks)
            {
                if (!Destroyed) Destroy();
                return;
            }

            base.Tick();
            age++;
            Position = caster.Position;

            if (this.Spawned && this.Map != null)
            {
                ClearProjectiles();
                if (age % 10 == 0)
                {
                    ApplyAreaDamage();
                }
            }
        }

        private void ClearProjectiles()
        {
            Map currentMap = this.Map;
            if (currentMap == null) return;

            List<Thing> things = currentMap.listerThings.AllThings;
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing t = things[i];
                if (t is Projectile && t.Position.InHorDistOf(Position, Radius))
                {
                    t.Destroy();
                }
            }
        }

        private void ApplyAreaDamage()
        {
            Map map = caster?.Map;
            if (map == null) return;

            float damageAmount = 3f;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p != null && p != caster && !p.Dead && p.Position.InHorDistOf(Position, Radius))
                {
                    if (p.Faction != null && p.Faction.HostileTo(caster.Faction))
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, damageAmount, 0f, -1f, caster);
                        p.TakeDamage(dinfo);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null || lasers == null || lasers.Count == 0) return;

            Vector3 center = caster.DrawPos;
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            foreach (var laser in lasers)
            {
                if (laser == null) continue; 

                float alpha = CalculateAlpha(laser);
                if (alpha <= 0) continue;

                Color color = Color.white;
                color.a = alpha;
                propBlock.SetColor("_Color", color);

                float currentAngle = laser.startAngle + (age * laser.rotateSpeed);
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(Mathf.Cos(rad) * 1.5f, 0f, Mathf.Sin(rad) * 1.5f);
                Vector3 pos = center + offset;

                Vector3 dir = (pos - center).normalized;
                if (dir == Vector3.zero) dir = Vector3.forward; 

                Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, -90f, 0f);

                float length = 40f;
                float width = 20f;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    pos + (dir * length / 2.5f),
                    rot,
                    new Vector3(length, 1f, width)
                );

                Graphics.DrawMesh(MeshPool.plane10, matrix, LaserMat, 0, null, 0, propBlock);
            }
        }

        private float CalculateAlpha(LaserData laser)
        {
            if (age < 30 + laser.fadeInDelay)
            {
                float t = (float)(age - laser.fadeInDelay) / 30f;
                return Mathf.Clamp01(t);
            }
            if (age > DurationTicks - 30 - laser.fadeOutDelay)
            {
                float t = (float)(DurationTicks - age - laser.fadeOutDelay) / 30f;
                return Mathf.Clamp01(t);
            }
            return 1f;
        }
    }
}